using MidiPadDaemon.Core.Interfaces;
using MidiPadDaemon.Core.Models;

namespace MidiPadDaemon.App.Endpoints;

public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/config").WithTags("Configuration");

        group.MapGet("/", GetConfig)
            .WithName("GetConfig")
            .WithSummary("Get the current configuration");

        group.MapPut("/", UpdateConfig)
            .WithName("UpdateConfig")
            .WithSummary("Replace the entire configuration");

        group.MapGet("/profiles", GetProfiles)
            .WithName("GetProfiles")
            .WithSummary("Get all profiles");

        group.MapPut("/profiles/active", SetActiveProfile)
            .WithName("SetActiveProfile")
            .WithSummary("Set the active profile by ID");
    }

    private static async Task<IResult> GetConfig(IConfigStore configStore, ILogger<Program> logger)
    {
        logger.LogDebug("GET /config");
        var config = await configStore.LoadAsync();
        return Results.Ok(config);
    }

    private static async Task<IResult> UpdateConfig(
        MidiConfig newConfig,
        IConfigStore configStore,
        IMappingEngine mappingEngine,
        ILogger<Program> logger)
    {
        logger.LogInformation("PUT /config - Updating configuration");

        await configStore.SaveAsync(newConfig);
        mappingEngine.UpdateConfig(newConfig);

        logger.LogInformation("Configuration updated successfully");
        return Results.NoContent();
    }

    private static async Task<IResult> GetProfiles(IConfigStore configStore, ILogger<Program> logger)
    {
        logger.LogDebug("GET /config/profiles");
        var config = await configStore.LoadAsync();
        return Results.Ok(config.Profiles.Select(p => new { p.Id, p.Name, BindingCount = p.Bindings.Count }));
    }

    private static async Task<IResult> SetActiveProfile(
        string profileId,
        IConfigStore configStore,
        IMappingEngine mappingEngine,
        ILogger<Program> logger)
    {
        logger.LogInformation("PUT /config/profiles/active - Setting active profile to: {ProfileId}", profileId);

        var config = await configStore.LoadAsync();

        if (!config.Profiles.Any(p => p.Id == profileId))
        {
            logger.LogWarning("Profile not found: {ProfileId}", profileId);
            return Results.NotFound(new { error = $"Profile '{profileId}' not found" });
        }

        var updatedConfig = config with { ActiveProfileId = profileId };
        await configStore.SaveAsync(updatedConfig);
        mappingEngine.UpdateConfig(updatedConfig);

        logger.LogInformation("Active profile set to: {ProfileId}", profileId);
        return Results.Ok(new { activeProfileId = profileId });
    }
}
