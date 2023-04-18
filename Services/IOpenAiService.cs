namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service interface to access Azure OpenAI.
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
    /// <param name="chatSessionId">Chat session identifier for the current conversation.</param>
    /// <param name="prompt">Prompt message to send to the deployment.</param>
    /// <returns>Response from the AI model deployment.</returns>
    Task<string> AskAsync(string chatSessionId, string prompt);
}