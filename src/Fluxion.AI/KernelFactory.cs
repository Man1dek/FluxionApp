using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Fluxion.AI;

/// <summary>
/// Builds and configures the Semantic Kernel instance for Fluxion.
/// Supports both Azure OpenAI and OpenAI backends.
/// </summary>
public static class KernelFactory
{
    /// <summary>
    /// Creates a Kernel wired to Azure OpenAI.
    /// </summary>
    public static Kernel CreateAzureOpenAI(
        string deploymentName,
        string endpoint,
        string apiKey)
    {
        var builder = Kernel.CreateBuilder();

        builder.AddAzureOpenAIChatCompletion(
            deploymentName: deploymentName,
            endpoint: endpoint,
            apiKey: apiKey);

        // Import Fluxion's custom plugins
        builder.Plugins.AddFromType<Plugins.CurriculumPlugin>();

        return builder.Build();
    }

    /// <summary>
    /// Creates a Kernel wired to OpenAI directly.
    /// </summary>
    public static Kernel CreateOpenAI(
        string modelId,
        string apiKey)
    {
        var builder = Kernel.CreateBuilder();

        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey);

        builder.Plugins.AddFromType<Plugins.CurriculumPlugin>();

        return builder.Build();
    }

    /// <summary>
    /// Registers the Semantic Kernel into the DI container.
    /// Call from Program.cs.
    /// </summary>
    public static IServiceCollection AddFluxionAI(
        this IServiceCollection services,
        string provider,          // "AzureOpenAI" or "OpenAI"
        string modelOrDeployment,
        string endpointOrApiKey,
        string? apiKey = null)
    {
        services.AddSingleton<Kernel>(sp =>
        {
            return provider switch
            {
                "AzureOpenAI" => CreateAzureOpenAI(modelOrDeployment, endpointOrApiKey, apiKey!),
                "OpenAI" => CreateOpenAI(modelOrDeployment, endpointOrApiKey),
                _ => throw new ArgumentException($"Unknown AI provider: {provider}")
            };
        });

        services.AddScoped<MorphingEngine>();
        services.AddSingleton<CognitiveLoadAnalyzer>();

        return services;
    }
}
