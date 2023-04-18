namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service interface to access AI back-end.
/// </summary>
public interface IOpenAiService
{

    /// <summary>
    /// Gets the maximum number of tokens.
    /// </summary>
    int MaxTokens { get; }

    /// <summary>
    /// Sends a prompt to the AI model deployment and returns the response.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="prompt">Prompt message to send to the deployment.</param>
    /// <returns>Response from the AI model deployment.</returns>
    Task<(string response, int promptTokens, int responseTokens)> AskAsync(string sessionId, string prompt) => Task.FromResult<(string, int, int)>((String.Empty, default, default));

    /// <summary>
    /// Sends the existing conversation to the AI model deployment and returns a brief summary.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="prompt">Prompt conversation to send to the deployment.</param>
    /// <returns>Summarization response from the AI model deployment.</returns>
    Task<string> SummarizeAsync(string sessionId, string prompt) => Task.FromResult<string>(String.Empty);
}