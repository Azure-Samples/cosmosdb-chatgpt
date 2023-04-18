using Cosmos.Chat.GPT.Models;

namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service interface to access Azure Cosmos DB for NoSQL.
/// </summary>
public interface ICosmosService
{

    /// <summary>
    /// Gets a list of all current chat sessions.
    /// </summary>
    /// <returns>List of distinct chat session items.</returns>
    Task<List<ChatSession>> GetChatSessionsAsync();

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    /// <param name="chatSession">Chat session item to create.</param>
    /// <returns>Newly created chat session item.</returns>
    Task<ChatSession?> InsertChatSessionAsync(ChatSession chatSession);

    /// <summary>
    /// Updates an existing chat session.
    /// </summary>
    /// <param name="chatSession">Chat session item to update.</param>
    /// <returns>Revised created chat session item.</returns>
    Task<ChatSession?> UpdateChatSessionAsync(ChatSession chatSession);

    /// <summary>
    /// Batch deletes an existing chat session and all related messages.
    /// </summary>
    /// <param name="chatSessionId">Chat session identifier used to flag messages and sessions for deletion.</param>
    Task DeleteChatSessionAndMessagesAsync(string chatSessionId);

    /// <summary>
    /// Creates a new chat message.
    /// </summary>
    /// <param name="chatMessage">Chat message item to create.</param>
    /// <returns>Newly created chat message item.</returns>
    Task<ChatMessage?> InsertChatMessageAsync(ChatMessage chatMessage);

    /// <summary>
    /// Gets a list of all current chat messages for a specified session identifier.
    /// </summary>
    /// <param name="chatSessionId">Chat session identifier used to filter messsages.</param>
    /// <returns>List of chat message items for the specified session.</returns>
    Task<List<ChatMessage>> GetChatSessionMessagesAsync(string chatSessionId);
}