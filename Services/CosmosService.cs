using Cosmos.Chat.GPT.Models;
using Microsoft.Azure.Cosmos;

namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service to access Azure Cosmos DB for NoSQL.
/// </summary>
public class CosmosService : ICosmosService
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
    public CosmosService(string endpoint, string key, string databaseName, string containerName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNullOrEmpty(databaseName);
        ArgumentNullException.ThrowIfNullOrEmpty(containerName);

        CosmosClient client = new(endpoint, key);
        Database? database = client?.GetDatabase(databaseName);
        Container? container = database?.GetContainer(containerName);

        _container = container ??
            throw new ArgumentException("Unable to connect to existing Azure Cosmos DB container or database.");
    }

    /// <summary>
    /// Gets a list of all current chat sessions.
    /// </summary>
    /// <returns>List of distinct chat session items.</returns>
    public async Task<List<ChatSession>> GetChatSessionsAsync()
    {
        QueryDefinition query = new QueryDefinition("SELECT DISTINCT c.id, c.Type, c.ChatSessionId, c.ChatSessionName FROM c WHERE c.Type = @Type")
            .WithParameter("@Type", "ChatSession");

        FeedIterator<ChatSession> response = _container.GetItemQueryIterator<ChatSession>(query);

        List<ChatSession> output = new();
        while (response.HasMoreResults)
        {
            FeedResponse<ChatSession> results = await response.ReadNextAsync();
            foreach (ChatSession chatSession in results)
            {
                output.Add(chatSession);
            }
        }
        return output;
    }

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    /// <param name="chatSession">Chat session item to create.</param>
    /// <returns>Newly created chat session item.</returns>
    public async Task<ChatSession?> InsertChatSessionAsync(ChatSession chatSession)
    {
        PartitionKey partitionKey = new(chatSession.ChatSessionId);
        return await _container.CreateItemAsync<ChatSession>(
            item: chatSession,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Updates an existing chat session.
    /// </summary>
    /// <param name="chatSession">Chat session item to update.</param>
    /// <returns>Revised created chat session item.</returns>
    public async Task<ChatSession?> UpdateChatSessionAsync(ChatSession chatSession)
    {
        PartitionKey partitionKey = new(chatSession.ChatSessionId);
        return await _container.ReplaceItemAsync(
            item: chatSession,
            id: chatSession.Id,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Batch deletes an existing chat session and all related messages.
    /// </summary>
    /// <param name="chatSessionId">Chat session identifier used to flag messages and sessions for deletion.</param>
    public async Task DeleteChatSessionAndMessagesAsync(string chatSessionId)
    {
        PartitionKey partitionKey = new(chatSessionId);

        QueryDefinition query = new QueryDefinition("SELECT c.id, c.ChatSessionId FROM c WHERE c.ChatSessionId = @chatSessionId")
                .WithParameter("@chatSessionId", chatSessionId);

        FeedIterator<ChatMessage> response = _container.GetItemQueryIterator<ChatMessage>(query);

        TransactionalBatch batch = _container.CreateTransactionalBatch(partitionKey);
        while (response.HasMoreResults)
        {
            FeedResponse<ChatMessage> results = await response.ReadNextAsync();
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
    /// <param name="chatMessage">Chat message item to create.</param>
    /// <returns>Newly created chat message item.</returns>
    public async Task<ChatMessage?> InsertChatMessageAsync(ChatMessage chatMessage)
    {
        PartitionKey partitionKey = new(chatMessage.ChatSessionId);
        return await _container.CreateItemAsync<ChatMessage>(
            item: chatMessage,
            partitionKey: partitionKey
        );
    }

    /// <summary>
    /// Gets a list of all current chat messages for a specified session identifier.
    /// </summary>
    /// <param name="chatSessionId">Chat session identifier used to filter messsages.</param>
    /// <returns>List of chat message items for the specified session.</returns>
    public async Task<List<ChatMessage>> GetChatSessionMessagesAsync(string chatSessionId)
    {
        QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.ChatSessionId = @ChatSessionId AND c.Type = @Type")
            .WithParameter("@ChatSessionId", chatSessionId)
            .WithParameter("@Type", "ChatMessage");

        FeedIterator<ChatMessage> results = _container.GetItemQueryIterator<ChatMessage>(query);

        List<ChatMessage> chatMessages = new();
        while (results.HasMoreResults)
        {
            FeedResponse<ChatMessage> response = await results.ReadNextAsync();
            foreach (ChatMessage chatMessage in response)
            {
                chatMessages.Add(chatMessage);
            }
        }
        return chatMessages;
    }
}