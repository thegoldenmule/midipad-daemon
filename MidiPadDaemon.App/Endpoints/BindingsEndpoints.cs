using MidiPadDaemon.Core.Interfaces;
using MidiPadDaemon.Core.Models;

namespace MidiPadDaemon.App.Endpoints;

public static class BindingsEndpoints
{
    public static void MapBindingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bindings").WithTags("Bindings");

        group.MapGet("/", GetAllBindings)
            .WithName("GetAllBindings")
            .WithSummary("Get all bindings in the active profile");

        group.MapGet("/{id}", GetBinding)
            .WithName("GetBinding")
            .WithSummary("Get a specific binding by ID");

        group.MapPost("/", CreateBinding)
            .WithName("CreateBinding")
            .WithSummary("Create a new binding in the active profile");

        group.MapPut("/{id}", UpdateBinding)
            .WithName("UpdateBinding")
            .WithSummary("Replace a binding by ID");

        group.MapDelete("/{id}", DeleteBinding)
            .WithName("DeleteBinding")
            .WithSummary("Delete a binding by ID");
    }

    private static async Task<IResult> GetAllBindings(IConfigStore configStore, ILogger<Program> logger)
    {
        logger.LogDebug("GET /bindings");
        var config = await configStore.LoadAsync();
        var profile = config.Profiles.FirstOrDefault(p => p.Id == config.ActiveProfileId);

        if (profile is null)
        {
            return Results.Ok(Array.Empty<MidiBinding>());
        }

        return Results.Ok(profile.Bindings);
    }

    private static async Task<IResult> GetBinding(string id, IConfigStore configStore, ILogger<Program> logger)
    {
        logger.LogDebug("GET /bindings/{Id}", id);
        var config = await configStore.LoadAsync();
        var profile = config.Profiles.FirstOrDefault(p => p.Id == config.ActiveProfileId);

        var binding = profile?.Bindings.FirstOrDefault(b => b.Id == id);
        if (binding is null)
        {
            logger.LogWarning("Binding not found: {Id}", id);
            return Results.NotFound(new { error = $"Binding '{id}' not found" });
        }

        return Results.Ok(binding);
    }

    private static async Task<IResult> CreateBinding(
        MidiBinding binding,
        IConfigStore configStore,
        IMappingEngine mappingEngine,
        ILogger<Program> logger)
    {
        logger.LogInformation("POST /bindings - Creating binding: {Id}", binding.Id);

        var config = await configStore.LoadAsync();
        var profileIndex = config.Profiles.FindIndex(p => p.Id == config.ActiveProfileId);

        if (profileIndex < 0)
        {
            return Results.BadRequest(new { error = "No active profile found" });
        }

        var profile = config.Profiles[profileIndex];

        // Check for duplicate ID
        if (profile.Bindings.Any(b => b.Id == binding.Id))
        {
            logger.LogWarning("Binding already exists: {Id}", binding.Id);
            return Results.Conflict(new { error = $"Binding '{binding.Id}' already exists" });
        }

        var updatedBindings = profile.Bindings.ToList();
        updatedBindings.Add(binding);

        var updatedProfile = profile with { Bindings = updatedBindings };
        var updatedProfiles = config.Profiles.ToList();
        updatedProfiles[profileIndex] = updatedProfile;

        var updatedConfig = config with { Profiles = updatedProfiles };
        await configStore.SaveAsync(updatedConfig);
        mappingEngine.UpdateConfig(updatedConfig);

        logger.LogInformation("Binding created: {Id}", binding.Id);
        return Results.Created($"/bindings/{binding.Id}", binding);
    }

    private static async Task<IResult> UpdateBinding(
        string id,
        MidiBinding binding,
        IConfigStore configStore,
        IMappingEngine mappingEngine,
        ILogger<Program> logger)
    {
        logger.LogInformation("PUT /bindings/{Id} - Updating binding", id);

        var config = await configStore.LoadAsync();
        var profileIndex = config.Profiles.FindIndex(p => p.Id == config.ActiveProfileId);

        if (profileIndex < 0)
        {
            return Results.BadRequest(new { error = "No active profile found" });
        }

        var profile = config.Profiles[profileIndex];
        var bindingIndex = profile.Bindings.FindIndex(b => b.Id == id);

        if (bindingIndex < 0)
        {
            logger.LogWarning("Binding not found: {Id}", id);
            return Results.NotFound(new { error = $"Binding '{id}' not found" });
        }

        // Ensure the binding ID matches the URL
        var updatedBinding = binding with { Id = id };

        var updatedBindings = profile.Bindings.ToList();
        updatedBindings[bindingIndex] = updatedBinding;

        var updatedProfile = profile with { Bindings = updatedBindings };
        var updatedProfiles = config.Profiles.ToList();
        updatedProfiles[profileIndex] = updatedProfile;

        var updatedConfig = config with { Profiles = updatedProfiles };
        await configStore.SaveAsync(updatedConfig);
        mappingEngine.UpdateConfig(updatedConfig);

        logger.LogInformation("Binding updated: {Id}", id);
        return Results.Ok(updatedBinding);
    }

    private static async Task<IResult> DeleteBinding(
        string id,
        IConfigStore configStore,
        IMappingEngine mappingEngine,
        ILogger<Program> logger)
    {
        logger.LogInformation("DELETE /bindings/{Id} - Deleting binding", id);

        var config = await configStore.LoadAsync();
        var profileIndex = config.Profiles.FindIndex(p => p.Id == config.ActiveProfileId);

        if (profileIndex < 0)
        {
            return Results.BadRequest(new { error = "No active profile found" });
        }

        var profile = config.Profiles[profileIndex];
        var bindingIndex = profile.Bindings.FindIndex(b => b.Id == id);

        if (bindingIndex < 0)
        {
            logger.LogWarning("Binding not found: {Id}", id);
            return Results.NotFound(new { error = $"Binding '{id}' not found" });
        }

        var updatedBindings = profile.Bindings.ToList();
        updatedBindings.RemoveAt(bindingIndex);

        var updatedProfile = profile with { Bindings = updatedBindings };
        var updatedProfiles = config.Profiles.ToList();
        updatedProfiles[profileIndex] = updatedProfile;

        var updatedConfig = config with { Profiles = updatedProfiles };
        await configStore.SaveAsync(updatedConfig);
        mappingEngine.UpdateConfig(updatedConfig);

        logger.LogInformation("Binding deleted: {Id}", id);
        return Results.NoContent();
    }
}
