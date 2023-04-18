using Cosmos.Chat.GPT.Models;

namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service interface to access data back-end.
/// </summary>
public interface ICosmosDbService
{

    /// <summary>
    /// Gets a list of all current chat sessions.
    /// </summary>
    /// <returns>List of distinct chat session items.</returns>
    Task<List<Session>> GetSessionsAsync() => Task.FromResult<List<Session>>(Enumerable.Empty<Session>().ToList());

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    /// <param name="session">Chat session item to create.</param>
    /// <returns>Newly created chat session item.</returns>
    Task<Session?> InsertSessionAsync(Session session) => Task.FromResult<Session?>(default);

    /// <summary>
    /// Updates an existing chat session.
    /// </summary>
    /// <param name="session">Chat session item to update.</param>
    /// <returns>Revised created chat session item.</returns>
    Task<Session?> UpdateSessionAsync(Session session) => Task.FromResult<Session?>(default);

    /// <summary>
    /// Batch deletes an existing chat session and all related messages.
    /// </summary>
    /// <param name="sessionId">Chat session identifier used to flag messages and sessions for deletion.</param>
    Task DeleteSessionAndMessagesAsync(string sessionId) => Task.CompletedTask;

    /// <summary>
    /// Creates a new chat message.
    /// </summary>
    /// <param name="message">Chat message item to create.</param>
    /// <returns>Newly created chat message item.</returns>
    Task<Message?> InsertMessageAsync(Message message) => Task.FromResult<Message?>(default);

    /// <summary>
    /// Gets a list of all current chat messages for a specified session identifier.
    /// </summary>
    /// <param name="sessionId">Chat session identifier used to filter messsages.</param>
    /// <returns>List of chat message items for the specified session.</returns>
    Task<List<Message>> GetSessionMessagesAsync(string sessionId) => Task.FromResult<List<Message>>(Enumerable.Empty<Message>().ToList());
}