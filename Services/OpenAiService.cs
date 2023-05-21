using Cosmos.Chat.GPT.Models;

namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service to access Azure OpenAI.
/// </summary>
public class OpenAiService
{
    private readonly string _deploymentName = String.Empty;
    private readonly int _maxConversationTokens = default;

    /// <summary>
    /// Gets the maximum number of tokens to limit chat conversation length.
    /// </summary>
    public int MaxConversationTokens
    {
        get => _maxConversationTokens;
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
    public OpenAiService(string endpoint, string key, string deploymentName, string maxConversationTokens)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(deploymentName);
        ArgumentNullException.ThrowIfNullOrEmpty(maxConversationTokens);
    }

    /// <summary>
    /// Sends a prompt to the deployed OpenAI LLM model and returns the response.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="prompt">Prompt message to send to the deployment.</param>
    /// <returns>Response from the OpenAI model along with tokens for the prompt and response.</returns>
    public async Task<(string response, int promptTokens, int responseTokens)> GetChatCompletionAsync(string sessionId, string userPrompt)
    {
        await Task.Delay(millisecondsDelay: 500);
        return ("&ltRESPONSE&gt;", 0, 0);
    }

    /// <summary>
    /// Sends the existing conversation to the OpenAI model and returns a two word summary.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="conversation">Prompt conversation to send to the deployment.</param>
    /// <returns>Summarization response from the OpenAI model deployment.</returns>
    public async Task<string> SummarizeAsync(string sessionId, string userPrompt)
    {
        await Task.Delay(millisecondsDelay: 500);
        return "&ltSUMMARY&gt;";
    }
}
