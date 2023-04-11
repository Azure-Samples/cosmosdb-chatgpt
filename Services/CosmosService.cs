using Cosmos.Chat.Models;
using Microsoft.Azure.Cosmos;

namespace Cosmos.Chat.Services;

public class CosmosService
{
    private readonly string _endpoint;
    private readonly string _key;
    private readonly string _databaseName;
    private readonly string _containerName;

    private readonly Container _container;

    public CosmosService(string endpoint, string key, string databaseName, string containerName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNullOrEmpty(databaseName);
        ArgumentNullException.ThrowIfNullOrEmpty(containerName);

        _endpoint = endpoint;
        _key = key;
        _databaseName = databaseName;
        _containerName = containerName;

        CosmosClient client = new(_endpoint, _key);
        Database? database = client?.GetDatabase(_databaseName);
        Container? container = database?.GetContainer(_containerName);

        _container = container ??
            throw new ArgumentException("Unable to connect to existing Azure Cosmos DB container or database.");
    }


    // First call is made to this when chat page is loaded for left-hand nav.
    // Only retrieve the chat sessions, not chat messages
    public async Task<List<ChatSession>> GetChatSessionsAsync()
    {

        List<ChatSession> chatSessions = new();

        try
        {
            //Get the chat sessions without the chat messages.
            QueryDefinition query = new QueryDefinition("SELECT DISTINCT c.id, c.Type, c.ChatSessionId, c.ChatSessionName FROM c WHERE c.Type = @Type")
                .WithParameter("@Type", "ChatSession");

            FeedIterator<ChatSession> results = _container.GetItemQueryIterator<ChatSession>(query);

            while (results.HasMoreResults)
            {
                FeedResponse<ChatSession> response = await results.ReadNextAsync();
                foreach (ChatSession chatSession in response)
                {
                    chatSessions.Add(chatSession);
                }

            }
        }
        catch (CosmosException ce)
        {
            //if 404, first run, create a new default chat session.
            if (ce.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ChatSession chatSession = new();
                await InsertChatSessionAsync(chatSession);
                chatSessions.Add(chatSession);
            }

        }

        return chatSessions;

    }

    public async Task<ChatSession> InsertChatSessionAsync(ChatSession chatSession)
    {

        return await _container.CreateItemAsync<ChatSession>(chatSession, new PartitionKey(chatSession.ChatSessionId));

    }

    public async Task<ChatSession> UpdateChatSessionAsync(ChatSession chatSession)
    {

        return await _container.ReplaceItemAsync(item: chatSession, id: chatSession.Id, partitionKey: new PartitionKey(chatSession.ChatSessionId));

    }

    public async Task DeleteChatSessionAsync(string chatSessionId)
    {

        //Retrieve the chat session and all chat messages
        QueryDefinition query = new QueryDefinition("SELECT c.id, c.ChatSessionId FROM c WHERE c.ChatSessionId = @chatSessionId")
                .WithParameter("@chatSessionId", chatSessionId);


        FeedIterator<ChatMessage> results = _container.GetItemQueryIterator<ChatMessage>(query);


        List<Task> deleteTasks = new();

        while (results.HasMoreResults)
        {
            FeedResponse<ChatMessage> response = await results.ReadNextAsync();

            foreach (var item in response)
            {

                deleteTasks.Add(_container.DeleteItemStreamAsync(item.Id, new PartitionKey(item.ChatSessionId)));

            }

        }

        await Task.WhenAll(deleteTasks);

    }

    public async Task<ChatMessage> InsertChatMessageAsync(ChatMessage chatMessage)
    {

        return await _container.CreateItemAsync<ChatMessage>(chatMessage, new PartitionKey(chatMessage.ChatSessionId));

    }

    public async Task<List<ChatMessage>> GetChatSessionMessagesAsync(string chatSessionId)
    {

        //Get the chat messages for a chat session
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