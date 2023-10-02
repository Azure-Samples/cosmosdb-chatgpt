using Cosmos.Chat.GPT.Models;

namespace Cosmos.Chat.GPT.Services;

public class CosmosDbService
{
    public CosmosDbService(string endpoint, string key, string databaseName, string containerName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(databaseName);
        ArgumentNullException.ThrowIfNullOrEmpty(containerName);
    }

    public async Task<Session> InsertSessionAsync(Session session)
    {
        await Task.Delay(millisecondsDelay: 500);
        return session;
    }

    public async Task<Message> InsertMessageAsync(Message message)
    {
        await Task.Delay(millisecondsDelay: 500);
        return message;
    }

    public async Task<List<Session>> GetSessionsAsync()
    {
        await Task.Delay(millisecondsDelay: 500);
        return Enumerable.Empty<Session>().ToList();
    }

    public async Task<List<Message>> GetSessionMessagesAsync(string sessionId)
    {
        await Task.Delay(millisecondsDelay: 500);
        return Enumerable.Empty<Message>().ToList();
    }

    public async Task<Session> UpdateSessionAsync(Session session)
    {
        await Task.Delay(millisecondsDelay: 500);
        return session;
    }

    public async Task UpsertSessionBatchAsync(params dynamic[] messages)
    {
        await Task.Delay(millisecondsDelay: 500);
    }

    public async Task DeleteSessionAndMessagesAsync(string sessionId)
    {
        await Task.Delay(millisecondsDelay: 500);
    }
}