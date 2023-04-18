using Cosmos.Chat.GPT.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service to access Azure Cosmos DB for NoSQL.
/// </summary>
public class CosmosDbService : ICosmosDbService
{
    private readonly Container _container;

    /// <summary>
    /// Creates a new instance of the service.
    /// </summary>
    /// <param name="endpoint">Endpoint URI.</param>
    /// <param name="key">Account key.</param>
    /// <param name="databaseName">Name of the database to access.</param>
    /// <param name="containerName">Name of the container to access.</param>
    /// <exception cref="ArgumentNullException">Thrown when endpoint, key, databaseName, or containerName is either null or empty.</exception>
    /// <remarks>
    /// This constructor will validate credentials and create a service client instance.
    /// </remarks>
    public CosmosDbService(string endpoint, string key, string databaseName, string containerName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNullOrEmpty(databaseName);
        ArgumentNullException.ThrowIfNullOrEmpty(containerName);

        CosmosSerializationOptions options = new()
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        };

        CosmosClient client = new CosmosClientBuilder(endpoint, key)
            .WithSerializerOptions(options)
            .Build();

        Database? database = client?.GetDatabase(databaseName);
        Container? container = database?.GetContainer(containerName);

        _container = container ??
            throw new ArgumentException("Unable to connect to existing Azure Cosmos DB container or database.");
    }

    /// <summary>
    /// Gets a list of all current chat sessions.
    /// </summary>
    /// <returns>List of distinct chat session items.</returns>
    public async Task<List<Session>> GetSessionsAsync()
    {
        QueryDefinition query = new QueryDefinition("SELECT DISTINCT c.id, c.Type, c.SessionId, c.Name FROM c WHERE c.Type = @Type")
            .WithParameter("@Type", nameof(Session));

        FeedIterator<Session> response = _container.GetItemQueryIterator<Session>(query);

        List<Session> output = new();
        while (response.HasMoreResults)
        {
            FeedResponse<Session> results = await response.ReadNextAsync();
            output.AddRange(results);
        }
        return output;
    }

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    /// <param name="session">Chat session item to create.</param>
    /// <returns>Newly created chat session item.</returns>
    public async Task<Session?> InsertSessionAsync(Session session)
    {
        PartitionKey partitionKey = new(session.SessionId);
        return await _container.CreateItemAsync<Session>(
            item: session,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Updates an existing chat session.
    /// </summary>
    /// <param name="session">Chat session item to update.</param>
    /// <returns>Revised created chat session item.</returns>
    public async Task<Session?> UpdateSessionAsync(Session session)
    {
        PartitionKey partitionKey = new(session.SessionId);
        return await _container.ReplaceItemAsync(
            item: session,
            id: session.Id,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Batch deletes an existing chat session and all related messages.
    /// </summary>
    /// <param name="sessionId">Chat session identifier used to flag messages and sessions for deletion.</param>
    public async Task DeleteSessionAndMessagesAsync(string sessionId)
    {
        PartitionKey partitionKey = new(sessionId);

        QueryDefinition query = new QueryDefinition("SELECT c.id, c.SessionId FROM c WHERE c.SessionId = @sessionId")
                .WithParameter("@sessionId", sessionId);

        FeedIterator<Message> response = _container.GetItemQueryIterator<Message>(query);

        TransactionalBatch batch = _container.CreateTransactionalBatch(partitionKey);
        while (response.HasMoreResults)
        {
            FeedResponse<Message> results = await response.ReadNextAsync();
            foreach (var item in results)
            {
                batch.DeleteItem(
                    id: item.Id
                );
            }
        }
        await batch.ExecuteAsync();
    }

    /// <summary>
    /// Creates a new chat message.
    /// </summary>
    /// <param name="message">Chat message item to create.</param>
    /// <returns>Newly created chat message item.</returns>
    public async Task<Message?> InsertMessageAsync(Message message)
    {
        PartitionKey partitionKey = new(message.SessionId);
        return await _container.CreateItemAsync<Message>(
            item: message,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Gets a list of all current chat messages for a specified session identifier.
    /// </summary>
    /// <param name="sessionId">Chat session identifier used to filter messsages.</param>
    /// <returns>List of chat message items for the specified session.</returns>
    public async Task<List<Message>> GetSessionMessagesAsync(string sessionId)
    {
        QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.SessionId = @ChatSessionId AND c.Type = @Type")
            .WithParameter("@ChatSessionId", sessionId)
            .WithParameter("@Type", nameof(Message));

        FeedIterator<Message> results = _container.GetItemQueryIterator<Message>(query);

        List<Message> output = new();
        while (results.HasMoreResults)
        {
            FeedResponse<Message> response = await results.ReadNextAsync();
            output.AddRange(response);
        }
        return output;
    }
}