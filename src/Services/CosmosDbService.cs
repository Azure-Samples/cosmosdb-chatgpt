using Cosmos.Chat.GPT.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using System.Collections.ObjectModel;

namespace Cosmos.Chat.GPT.Services;

public class CosmosDbService
{
    private readonly Container _chatContainer;
    private readonly Container _cacheContainer;

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
            .Build()!;

        Database database = client.GetDatabase(databaseName)!;
        Container chatContainer = database.GetContainer(chatContainerName)!;
        Container cacheContainer = CreateCacheContainer(database, cacheContainerName)!;

        _chatContainer = chatContainer ??
            throw new ArgumentException("Unable to connect to existing Azure Cosmos DB container or database.");
        _cacheContainer = cacheContainer ??
            throw new ArgumentException("Unable to connect to existing Azure Cosmos DB container or database.");
    }

    public async Task<string> CacheGetAsync(float[] vectors, double similarityScore)
    {
        string cacheResponse = "";

        string queryText = @"";

        var queryDef = new QueryDefinition(
                query: queryText)
            .WithParameter("@vectors", vectors)
            .WithParameter("@similarityScore", similarityScore);

        using FeedIterator<CacheItem> resultSet = _cacheContainer.GetItemQueryIterator<CacheItem>(queryDefinition: queryDef);

        while (resultSet.HasMoreResults)
        {
            FeedResponse<CacheItem> response = await resultSet.ReadNextAsync();
            foreach (CacheItem item in response)
            {
                cacheResponse = item.Completion;
                return cacheResponse;
            }
        }
        return cacheResponse;
    }

    private static Container CreateCacheContainer(Database database, string cacheContainerName)
    {
        ThroughputProperties throughputProperties = ThroughputProperties.CreateAutoscaleThroughput(4000);
        ContainerProperties properties = new ContainerProperties(id: cacheContainerName, partitionKeyPath: "/id")
        {
            DefaultTimeToLive = 86400,
            VectorEmbeddingPolicy = new(
            new Collection<Embedding>(
            [
                new Embedding()
                {
                    Path = "/vectors",
                    DataType = VectorDataType.Float32,
                    DistanceFunction = DistanceFunction.Cosine,
                    Dimensions = 1536
                }
            ])),
            IndexingPolicy = new IndexingPolicy()
            {
                // Define the vector index policy
                VectorIndexes = new()
                {
                    new VectorIndexPath()
                    {
                        Path = "/vectors",
                        Type = VectorIndexType.QuantizedFlat
                    }
                }
            }
        };
        Container container = database.CreateContainerIfNotExistsAsync(properties, throughputProperties).Result;
        return container;
    }

    public async Task<Session> InsertSessionAsync(Session session)
    {
        PartitionKey partitionKey = new(session.SessionId);
        return await _chatContainer.CreateItemAsync<Session>(
            item: session,
            partitionKey: partitionKey
        );
    }

    public async Task<Message> InsertMessageAsync(Message message)
    {
        PartitionKey partitionKey = new(message.SessionId);
        Message newMessage = message with { TimeStamp = DateTime.UtcNow };
        return await _chatContainer.CreateItemAsync<Message>(
            item: message,
            partitionKey: partitionKey
        );
    }

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

    public async Task UpsertSessionBatchAsync(params dynamic[] messages)
    {
        if (messages.Select(m => m.SessionId).Distinct().Count() > 1)
        {
            throw new ArgumentException("All items must have the same partition key.");
        }

        PartitionKey partitionKey = new(messages[0].SessionId);
        TransactionalBatch batch = _chatContainer.CreateTransactionalBatch(partitionKey);

        foreach (var message in messages)
        {
            batch.UpsertItem(item: message);
        }
        await batch.ExecuteAsync();
    }

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

    public async Task CachePutAsync(CacheItem cacheItem)
    {
        await _cacheContainer.UpsertItemAsync<CacheItem>(item: cacheItem);
    }

    public async Task CacheRemoveAsync(float[] vectors)
    {
        double similarityScore = 0.99;
        string queryText = "SELECT Top 1 c.id FROM (SELECT c.id, VectorDistance(c.vectors, @vectors, false) as similarityScore FROM c) x WHERE x.similarityScore > @similarityScore ORDER BY x.similarityScore desc";

        var queryDef = new QueryDefinition(
             query: queryText)
            .WithParameter("@vectors", vectors)
            .WithParameter("@similarityScore", similarityScore);

        using FeedIterator<CacheItem> resultSet = _cacheContainer.GetItemQueryIterator<CacheItem>(queryDefinition: queryDef);

        while (resultSet.HasMoreResults)
        {
            FeedResponse<CacheItem> response = await resultSet.ReadNextAsync();

            foreach (CacheItem item in response)
            {
                await _cacheContainer.DeleteItemAsync<CacheItem>(partitionKey: new PartitionKey(item.Id), id: item.Id);
                return;
            }
        }
    }

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