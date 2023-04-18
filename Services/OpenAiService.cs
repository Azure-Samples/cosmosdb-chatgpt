using Azure;
using Azure.AI.OpenAI;

namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service to access Azure OpenAI.
/// </summary>
public class OpenAiService : IOpenAiService
{
    private readonly string _deploymentName = String.Empty;
    private readonly int _maxTokens = default;
    private readonly OpenAIClient _client;

    /// <summary>
    /// Gets the maximum number of tokens.
    /// </summary>
    public int MaxTokens
    {
        get => _maxTokens;
    }

    /// <summary>
    /// Creates a new instance of the service.
    /// </summary>
    /// <param name="endpoint">Endpoint URI.</param>
    /// <param name="key">Account key.</param>
    /// <param name="deploymentName">Name of the deployment access.</param>
    /// <param name="maxTokens">Maximum number of tokens per request.</param>
    /// <exception cref="ArgumentNullException">Thrown when endpoint, key, deploymentName, or maxTokens is either null or empty.</exception>
    /// <remarks>
    /// This constructor will validate credentials and create a HTTP client instance.
    /// </remarks>
    public OpenAiService(string endpoint, string key, string deploymentName, string maxTokens)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNullOrEmpty(deploymentName);
        ArgumentNullException.ThrowIfNullOrEmpty(maxTokens);

        _deploymentName = deploymentName;
        _maxTokens = Int32.TryParse(maxTokens, out _maxTokens) ? _maxTokens : 3000;

        _client = new(new Uri(endpoint), new AzureKeyCredential(key));
    }

    /// <summary>
    /// Sends a prompt to the AI model deployment and returns the response.
    /// </summary>
    /// <param name="chatSessionId">Chat session identifier for the current conversation.</param>
    /// <param name="prompt">Prompt message to send to the deployment.</param>
    /// <returns>Response from the AI model deployment.</returns>
    public async Task<string> AskAsync(string chatSessionId, string prompt)
    {
        CompletionsOptions completionsOptions = new()
        {
            User = chatSessionId,
            MaxTokens = _maxTokens
        };
        completionsOptions.Prompts.Add(prompt);

        Response<Completions> completionsResponse = await _client.GetCompletionsAsync(_deploymentName, completionsOptions);

        return completionsResponse.Value.Choices[0].Text;
    }
}