using System.Text;
using MidiPadDaemon.Core.Interfaces;
using MidiPadDaemon.Core.Models;
using Microsoft.Extensions.Logging;

namespace MidiPadDaemon.Core.Executors;

/// <summary>
/// Executes HTTP request actions.
/// Supports basic templating of body with MIDI event properties.
/// </summary>
public sealed class HttpActionExecutor : IActionExecutor
{
    private readonly ILogger<HttpActionExecutor> _logger;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _timeout;

    public HttpActionExecutor(
        ILogger<HttpActionExecutor> logger,
        HttpClient httpClient,
        TimeSpan? timeout = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _timeout = timeout ?? TimeSpan.FromSeconds(10);
    }

    public bool CanExecute(ActionConfig action) => action is HttpRequestActionConfig;

    public async Task ExecuteAsync(ActionConfig action, MidiInputEvent sourceEvent, CancellationToken ct = default)
    {
        if (action is not HttpRequestActionConfig http)
        {
            _logger.LogWarning("HttpActionExecutor received non-HTTP action");
            return;
        }

        _logger.LogDebug(
            "Executing HTTP request: {Method} {Url}",
            http.Method,
            http.Url);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeout);

            var request = new HttpRequestMessage(
                new HttpMethod(http.Method),
                http.Url);

            // Add custom headers
            if (http.Headers is not null)
            {
                foreach (var header in http.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    _logger.LogDebug("Added header: {Key}={Value}", header.Key, header.Value);
                }
            }

            // Add body with template substitution
            if (!string.IsNullOrEmpty(http.BodyTemplate))
            {
                var body = ApplyTemplate(http.BodyTemplate, sourceEvent);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                _logger.LogDebug("Request body: {Body}", body);
            }

            var response = await _httpClient.SendAsync(request, cts.Token);

            _logger.LogInformation(
                "HTTP request completed: {Method} {Url} -> {StatusCode}",
                http.Method,
                http.Url,
                (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning(
                    "HTTP request returned non-success status: {StatusCode} - {Body}",
                    response.StatusCode,
                    responseBody);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "HTTP request timed out after {Timeout}s: {Url}",
                _timeout.TotalSeconds,
                http.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error executing HTTP request: {Method} {Url}",
                http.Method,
                http.Url);
        }
    }

    /// <summary>
    /// Applies simple template substitution with MIDI event properties.
    /// Supports: {deviceId}, {channel}, {eventType}, {number}, {value}, {timestamp}
    /// </summary>
    private static string ApplyTemplate(string template, MidiInputEvent ev)
    {
        return template
            .Replace("{deviceId}", ev.DeviceId, StringComparison.OrdinalIgnoreCase)
            .Replace("{channel}", ev.Channel.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{eventType}", ev.EventType.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{number}", ev.Number.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{value}", ev.Value.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{timestamp}", ev.Timestamp.ToString("O"), StringComparison.OrdinalIgnoreCase);
    }
}
