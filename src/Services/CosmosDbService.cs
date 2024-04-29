using Cosmos.Chat.GPT.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service to access Azure Cosmos DB for NoSQL.
/// </summary>
public class CosmosDbService
{
    private readonly Container _chatContainer;
    private readonly Container _cacheContainer;

    /// <summary>
    /// Creates a new instance of the service.
    /// </summary>
    /// <param name="endpoint">Endpoint URI.</param>
    /// <param name="key">Account key.</param>
    /// <param name="databaseName">Name of the database to access.</param>
    /// <param name="chatContainerName">Name of the chat container to access.</param>
    /// <param name="cacheContainerName">Name of the cache container to access.</param>
    /// <exception cref="ArgumentNullException">Thrown when endpoint, key, databaseName, cacheContainername or chatContainerName is either null or empty.</exception>
    /// <remarks>
    /// This constructor will validate credentials and create a service client instance.
    /// </remarks>
    public CosmosDbService(string endpoint, string key, string databaseName, string chatContainerName, string cacheContainerName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNullOrEmpty(databaseName);
        ArgumentNullException.ThrowIfNullOrEmpty(chatContainerName);
        ArgumentNullException.ThrowIfNullOrEmpty(cacheContainerName);

        CosmosSerializationOptions options = new()
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        };

        CosmosClient client = new CosmosClientBuilder(endpoint, key)
            .WithSerializerOptions(options)
            .Build();

        Database? database = client?.GetDatabase(databaseName);
        Container? chatContainer = database?.GetContainer(chatContainerName);
        Container? cacheContainer = database?.GetContainer(cacheContainerName);

        _chatContainer = chatContainer ??
            throw new ArgumentException("Unable to connect to existing Azure Cosmos DB container or database.");

        _cacheContainer = cacheContainer ??
            throw new ArgumentException("Unable to connect to existing Azure Cosmos DB container or database.");
    }

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    /// <param name="session">Chat session item to create.</param>
    /// <returns>Newly created chat session item.</returns>
    public async Task<Session> InsertSessionAsync(Session session)
    {
        PartitionKey partitionKey = new(session.SessionId);
        return await _chatContainer.CreateItemAsync<Session>(
            item: session,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Creates a new chat message.
    /// </summary>
    /// <param name="message">Chat message item to create.</param>
    /// <returns>Newly created chat message item.</returns>
    public async Task<Message> InsertMessageAsync(Message message)
    {
        PartitionKey partitionKey = new(message.SessionId);
        Message newMessage = message with { TimeStamp = DateTime.UtcNow };
        return await _chatContainer.CreateItemAsync<Message>(
            item: message,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Gets a list of all current chat sessions.
    /// </summary>
    /// <returns>List of distinct chat session items.</returns>
    public async Task<List<Session>> GetSessionsAsync()
    {
        QueryDefinition query = new QueryDefinition("SELECT DISTINCT * FROM c WHERE c.type = @type")
            .WithParameter("@type", nameof(Session));

        FeedIterator<Session> response = _chatContainer.GetItemQueryIterator<Session>(query);

        List<Session> output = new();
        while (response.HasMoreResults)
        {
            FeedResponse<Session> results = await response.ReadNextAsync();
            output.AddRange(results);
        }
        return output;
    }

    /// <summary>
    /// Gets a list of all current chat messages for a specified session identifier.
    /// </summary>
    /// <param name="sessionId">Chat session identifier used to filter messsages.</param>
    /// <returns>List of chat message items for the specified session.</returns>
    public async Task<List<Message>> GetSessionMessagesAsync(string sessionId)
    {
        QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.sessionId = @sessionId AND c.type = @type")
            .WithParameter("@sessionId", sessionId)
            .WithParameter("@type", nameof(Message));

        FeedIterator<Message> results = _chatContainer.GetItemQueryIterator<Message>(query);

        List<Message> output = new();
        while (results.HasMoreResults)
        {
            FeedResponse<Message> response = await results.ReadNextAsync();
            output.AddRange(response);
        }
        return output;
    }

    /// <summary>
    /// Updates an existing chat session.
    /// </summary>
    /// <param name="session">Chat session item to update.</param>
    /// <returns>Revised created chat session item.</returns>
    public async Task<Session> UpdateSessionAsync(Session session)
    {
        PartitionKey partitionKey = new(session.SessionId);
        return await _chatContainer.ReplaceItemAsync(
            item: session,
            id: session.Id,
            partitionKey: partitionKey
        );
    }

    public async Task<Session> GetSessionAsync(string sessionId)
    {
        PartitionKey partitionKey = new(sessionId);
        return await _chatContainer.ReadItemAsync<Session>(
            partitionKey: partitionKey,
            id: sessionId
            );
    }

    /// <summary>
    /// Batch create chat message and update session.
    /// </summary>
    /// <param name="messages">Chat message and session items to create or replace.</param>
    public async Task UpsertSessionBatchAsync(params dynamic[] messages)
    {

        //Make sure items are all in the same partition
        if (messages.Select(m => m.SessionId).Distinct().Count() > 1)
        {
            throw new ArgumentException("All items must have the same partition key.");
        }

        PartitionKey partitionKey = new(messages[0].SessionId);
        TransactionalBatch batch = _chatContainer.CreateTransactionalBatch(partitionKey);

        foreach(var message in messages)
        {
            batch.UpsertItem(item: message);
        }
        
        await batch.ExecuteAsync();
    }

    /// <summary>
    /// Batch deletes an existing chat session and all related messages.
    /// </summary>
    /// <param name="sessionId">Chat session identifier used to flag messages and sessions for deletion.</param>
    public async Task DeleteSessionAndMessagesAsync(string sessionId)
    {
        PartitionKey partitionKey = new(sessionId);

        QueryDefinition query = new QueryDefinition("SELECT VALUE c.id FROM c WHERE c.sessionId = @sessionId")
                .WithParameter("@sessionId", sessionId);

        FeedIterator<string> response = _chatContainer.GetItemQueryIterator<string>(query);

        TransactionalBatch batch = _chatContainer.CreateTransactionalBatch(partitionKey);
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
    }

    /// <summary>
    /// Find a cache item.
    /// </summary>
    /// <param name="vectors">Vectors to do the semantic search in the cache.</param>
    /// <param name="similarityScore">Value to determine how similar the vectors >0.99 is exact match.</param>
    public async Task<string> CacheGetAsync(float[] vectors, double similarityScore)
    {
        string queryText = "SELECT Top 1 c.prompt, c.completion, x.similarityScore FROM(SELECT c.prompt, c.completion, VectorDistance(c.vectors, @vectors, false) as similarityScore FROM c) x WHERE x.similarityScore > @similarityScore ORDER BY x.similarityScore desc";

        var queryDef = new QueryDefinition(
             query: queryText)
            .WithParameter("@vectors", vectors)
            .WithParameter("@similarityScore", similarityScore);

        using FeedIterator<dynamic> resultSet = _cacheContainer.GetItemQueryIterator<dynamic>(queryDefinition: queryDef);

        string cacheResponse = "";

        while (resultSet.HasMoreResults)
        {
            FeedResponse<dynamic> response = await resultSet.ReadNextAsync();

            foreach (var item in response)
            {
                cacheResponse = item.completion;
            }
        }

        return cacheResponse;
    }

    /// <summary>
    /// Add a new cache item.
    /// </summary>
    /// <param name="vectors">Vectors used to perform the semantic search.</param>
    /// <param name="prompt">Text value of the vectors in the search.</param>
    /// <param name="completion">Text value of the previously generated response to return to the user.</param>
    public async Task CachePutAsync(float[] vectors, string prompts, string completion)
    {
        CacheItem cacheItem = new(vectors, prompts, completion);

        await _cacheContainer.UpsertItemAsync<CacheItem>(item: cacheItem);
    }

    /// <summary>
    /// Remove a cache item.
    /// </summary>
    /// <param name="vectors">Vectors used to perform the semantic search. Similarity Score is set to 0.99 for exact match</param>
    public async Task CacheRemoveAsync(float[] vectors)
    {
        double similarityScore = 0.99;
        string queryText = "SELECT Top 1 c.id FROM (SELECT c.id, VectorDistance(c.vectors, @vectors, false) as similarityScore FROM c) x WHERE x.similarityScore > @similarityScore ORDER BY x.similarityScore desc";

        var queryDef = new QueryDefinition(
             query: queryText)
            .WithParameter("@vectors", vectors)
            .WithParameter("@similarityScore", similarityScore);

        using FeedIterator<CacheItem> resultSet = _cacheContainer.GetItemQueryIterator<CacheItem>(queryDefinition: queryDef);

        string cacheId = "";

        while (resultSet.HasMoreResults)
        {
            FeedResponse<CacheItem> response = await resultSet.ReadNextAsync();

            foreach (CacheItem item in response)
            {
                cacheId = item.Id;
                await _cacheContainer.DeleteItemAsync<CacheItem>(partitionKey: new PartitionKey(cacheId), id: cacheId);
                return;
            }
        }
    }

    /// <summary>
    /// Clear the cache of all cache items.
    /// </summary>
    public async Task CacheClearAsync()
    {

        string queryText = "SELECT c.id FROM c";

        var queryDef = new QueryDefinition(query: queryText);

        using FeedIterator<CacheItem> resultSet = _cacheContainer.GetItemQueryIterator<CacheItem>(queryDefinition: queryDef);

        while (resultSet.HasMoreResults)
        {
            FeedResponse<CacheItem> response = await resultSet.ReadNextAsync();

            foreach (CacheItem item in response)
            {
                await _cacheContainer.DeleteItemAsync<CacheItem>(partitionKey: new PartitionKey(item.Id), id: item.Id);
            }
        }
    }
}