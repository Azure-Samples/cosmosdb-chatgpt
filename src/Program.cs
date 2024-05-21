using Cosmos.Chat.GPT.Options;
using Cosmos.Chat.GPT.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.RegisterConfiguration();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.RegisterServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();

static class ProgramExtensions
{
    public static void RegisterConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<CosmosDb>()
            .Bind(builder.Configuration.GetSection(nameof(CosmosDb)));

        builder.Services.AddOptions<OpenAi>()
            .Bind(builder.Configuration.GetSection(nameof(OpenAi)));

        builder.Services.AddOptions<Chat>()
            .Bind(builder.Configuration.GetSection(nameof(Chat)));
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<CosmosDbService, CosmosDbService>((provider) =>
        {
            var cosmosDbOptions = provider.GetRequiredService<IOptions<CosmosDb>>();
            if (cosmosDbOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<CosmosDb>)} was not resolved through dependency injection.");
            }
            else
            {
                return new CosmosDbService(
                    endpoint: cosmosDbOptions.Value?.Endpoint ?? String.Empty,
                    key: cosmosDbOptions.Value?.Key ?? String.Empty,
                    databaseName: cosmosDbOptions.Value?.Database ?? String.Empty,
                    chatContainerName: cosmosDbOptions.Value?.ChatContainer ?? String.Empty,
                    cacheContainerName: cosmosDbOptions.Value?.CacheContainer ?? String.Empty
                );
            }
        });
        services.AddSingleton<OpenAiService, OpenAiService>((provider) =>
        {
            var openAiOptions = provider.GetRequiredService<IOptions<OpenAi>>();
            if (openAiOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<OpenAi>)} was not resolved through dependency injection.");
            }
            else
            {
                return new OpenAiService(
                    endpoint: openAiOptions.Value?.Endpoint ?? String.Empty,
                    key: openAiOptions.Value?.Key ?? String.Empty,
                    completionDeploymentName: openAiOptions.Value?.CompletionDeploymentName ?? String.Empty,
                    embeddingDeploymentName: openAiOptions.Value?.EmbeddingDeploymentName ?? String.Empty
                );
            }
        });
        services.AddSingleton<SemanticKernelService, SemanticKernelService>((provider) =>
        {
            //We are using the same options as OpenAiService. There is an config options class for SemanticKernelService
            //but it is not used. Leaving here if you decide to use in the future. 
            var semanticKernalOptions = provider.GetRequiredService<IOptions<OpenAi>>();
            //var semanticKernalOptions = provider.GetRequiredService<IOptions<SemanticKernel>>();
            if (semanticKernalOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<OpenAi>)} was not resolved through dependency injection.");
                //throw new ArgumentException($"{nameof(IOptions<SemanticKernel>)} was not resolved through dependency injection.");
            }
            else
            {
                return new SemanticKernelService(
                    endpoint: semanticKernalOptions.Value?.Endpoint ?? String.Empty,
                    key: semanticKernalOptions.Value?.Key ?? String.Empty,
                    completionDeploymentName: semanticKernalOptions.Value?.CompletionDeploymentName ?? String.Empty,
                    embeddingDeploymentName: semanticKernalOptions.Value?.EmbeddingDeploymentName ?? String.Empty
                );
            }
        });
        services.AddSingleton<ChatService>((provider) =>
        {
            var chatOptions = provider.GetRequiredService<IOptions<Chat>>();
            if (chatOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<Chat>)} was not resolved through dependency injection.");
            }
            else
            {
                var cosmosDbService = provider.GetRequiredService<CosmosDbService>();
                var openAiService = provider.GetRequiredService<OpenAiService>();
                var semanticKernelService = provider.GetRequiredService<SemanticKernelService>();
                return new ChatService(
                    openAiService: openAiService,
                    cosmosDbService: cosmosDbService,
                    semanticKernelService: semanticKernelService,
                    maxConversationTokens: chatOptions.Value?.MaxConversationTokens ?? String.Empty,
                    cacheSimilarityScore: chatOptions.Value?.CacheSimilarityScore ?? String.Empty
                );
            }
        });
    }
}
