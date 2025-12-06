using System.Diagnostics;
using MidiPadDaemon.Core.Interfaces;
using MidiPadDaemon.Core.Models;
using Microsoft.Extensions.Logging;

namespace MidiPadDaemon.Core.Executors;

/// <summary>
/// Executes shell command actions.
/// Disabled by default for security - must be explicitly enabled.
/// </summary>
public sealed class ShellCommandActionExecutor : IActionExecutor
{
    private readonly ILogger<ShellCommandActionExecutor> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _timeout;

    public ShellCommandActionExecutor(
        ILogger<ShellCommandActionExecutor> logger,
        bool enabled = false,
        TimeSpan? timeout = null)
    {
        _logger = logger;
        _enabled = enabled;
        _timeout = timeout ?? TimeSpan.FromSeconds(30);

        if (_enabled)
        {
            _logger.LogWarning("ShellCommandActionExecutor is ENABLED - shell commands will be executed");
        }
        else
        {
            _logger.LogInformation("ShellCommandActionExecutor is disabled (default for security)");
        }
    }

    public bool CanExecute(ActionConfig action) => action is ShellCommandActionConfig;

    public async Task ExecuteAsync(ActionConfig action, MidiInputEvent sourceEvent, CancellationToken ct = default)
    {
        if (action is not ShellCommandActionConfig shell)
        {
            _logger.LogWarning("ShellCommandActionExecutor received non-shell action");
            return;
        }

        if (!_enabled)
        {
            _logger.LogWarning(
                "Shell command blocked (disabled): {Command} {Arguments}",
                shell.Command,
                shell.Arguments);
            return;
        }

        _logger.LogDebug(
            "Executing shell command: {Command} {Arguments}",
            shell.Command,
            shell.Arguments);

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = shell.Command,
                    Arguments = shell.Arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_timeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Shell command timed out after {Timeout}s: {Command}",
                    _timeout.TotalSeconds,
                    shell.Command);
                process.Kill(entireProcessTree: true);
                return;
            }

            var output = await outputTask;
            var error = await errorTask;

            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogDebug("Shell command output: {Output}", output.Trim());
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("Shell command stderr: {Error}", error.Trim());
            }

            _logger.LogInformation(
                "Shell command completed: {Command} (exit code: {ExitCode})",
                shell.Command,
                process.ExitCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error executing shell command: {Command}",
                shell.Command);
        }
    }
}
