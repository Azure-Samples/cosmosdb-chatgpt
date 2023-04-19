namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service to access Azure OpenAI.
/// </summary>
public class OpenAiService
{
    private readonly string _deploymentName = String.Empty;
    private readonly int _maxTokens = default;

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
        ArgumentNullException.ThrowIfNullOrEmpty(deploymentName);
        ArgumentNullException.ThrowIfNullOrEmpty(maxTokens);
    }

    /// <summary>
    /// Sends a prompt to the AI model deployment and returns the response.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="prompt">Prompt message to send to the deployment.</param>
    /// <returns>Response from the AI model deployment along with tokens for the prompt and response.</returns>
    public async Task<(string response, int promptTokens, int responseTokens)> AskAsync(string sessionId, string prompt)
    {
        await Task.Delay(millisecondsDelay: 500);
        return ("<RESPONSE>", 0, 0);
    }

    /// <summary>
    /// Sends the existing conversation to the AI model deployment and returns a brief summary.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="conversation">Prompt conversation to send to the deployment.</param>
    /// <returns>Summarization response from the AI model deployment.</returns>
    public async Task<string> SummarizeAsync(string sessionId, string prompt)
    {
        await Task.Delay(millisecondsDelay: 500);
        return "<SUMMARY>";
    }
}