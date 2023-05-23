using Cosmos.Chat.GPT.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace Cosmos.Chat.GPT.Services;

public class CosmosDbService
{
    private readonly Container _container = default!;

    public CosmosDbService(string endpoint, string key, string databaseName, string containerName)
    {
    }

    public async Task<List<Session>> GetSessionsAsync()
    {
        /*
        QueryDefinition query = new QueryDefinition("SELECT DISTINCT * FROM c WHERE c.type = @type")
            .WithParameter("@type", nameof(Session));

        FeedIterator<Session> response = _container.GetItemQueryIterator<Session>(query);

        List<Session> output = new();
        while (response.HasMoreResults)
        {
            FeedResponse<Session> results = await response.ReadNextAsync();
            output.AddRange(results);
        }
        return output;
        */
        
        // Remove this placeholder code
        await Task.Delay(millisecondsDelay: 500);
        return Enumerable.Empty<Session>().ToList();
    }

    public async Task<List<Message>> GetSessionMessagesAsync(string sessionId)
    {
        /*
        QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.sessionId = @sessionId AND c.type = @type")
            .WithParameter("@sessionId", sessionId)
            .WithParameter("@type", nameof(Message));
        FeedIterator<Message> response = _container.GetItemQueryIterator<Message>(query);
        List<Message> output = new();
        while (response.HasMoreResults)
        {
            FeedResponse<Message> results = await response.ReadNextAsync();
            output.AddRange(results);
        }
        return output;
        */
        
        // Remove this placeholder code
        await Task.Delay(millisecondsDelay: 500);
        return Enumerable.Empty<Message>().ToList();
    }

    public async Task<Session> InsertSessionAsync(Session session)
    {
        /*
        PartitionKey partitionKey = new(session.SessionId);
        return await _container.CreateItemAsync<Session>(
            item: session,
            partitionKey: partitionKey
        );
        */
        
        // Remove this placeholder code
        await Task.Delay(millisecondsDelay: 500);
        return session;
    }

    public async Task<Message> InsertMessageAsync(Message message)
    {
        /*
        PartitionKey partitionKey = new(message.SessionId);
        Message newMessage = message with { TimeStamp = DateTime.UtcNow };
        return await _container.CreateItemAsync<Message>(
            item: newMessage,
            partitionKey: partitionKey
        );
        */
        
        // Remove this placeholder code
        await Task.Delay(millisecondsDelay: 500);
        return message;
    }

    public async Task<Session> UpdateSessionAsync(Session session)
    {
        /*
        PartitionKey partitionKey = new(session.SessionId);
        return await _container.ReplaceItemAsync(
            item: session,
            id: session.Id,
            partitionKey: partitionKey
        );
        */
        
        // Remove this placeholder code
        await Task.Delay(millisecondsDelay: 500);
        return session;
    }

    public async Task UpsertSessionBatchAsync(params dynamic[] messages)
    {
        /*
        if (messages.Select(m => m.SessionId).Distinct().Count() > 1)
        {
            throw new ArgumentException("All items must have the same partition key.");
        }
        PartitionKey partitionKey = new(messages.First().SessionId);
        TransactionalBatch batch = _container.CreateTransactionalBatch(partitionKey);
        foreach (var message in messages)
        {
            batch.UpsertItem(
                item: message
            );
        }
        await batch.ExecuteAsync();
        */
        
        // Remove this placeholder code
        await Task.Delay(millisecondsDelay: 500);
    }

    public async Task DeleteSessionAndMessagesAsync(string sessionId)
    {
        /*
        PartitionKey partitionKey = new(sessionId);
        QueryDefinition query = new QueryDefinition("SELECT VALUE c.id FROM c WHERE c.sessionId = @sessionId")
            .WithParameter("@sessionId", sessionId);
        FeedIterator<string> response = _container.GetItemQueryIterator<string>(query);
        TransactionalBatch batch = _container.CreateTransactionalBatch(partitionKey);
        while (response.HasMoreResults)
        {
            FeedResponse<string> results = await response.ReadNextAsync();
            foreach (var itemId in results)
            {
                batch.DeleteItem(
                    id: itemId
                );
            }
        }
        await batch.ExecuteAsync();
        */
        
        // Remove this placeholder code
        await Task.Delay(millisecondsDelay: 500);
    }
}
