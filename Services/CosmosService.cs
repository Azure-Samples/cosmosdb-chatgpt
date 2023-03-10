using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Collections;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System;
using System.ComponentModel;
using Container = Microsoft.Azure.Cosmos.Container;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CosmosDB_ChatGPT.Services
{
    public class CosmosService
    {
        
        private readonly CosmosClient cosmosClient;
        private Container chatContainer;
        private readonly string databaseId;
        private readonly string containerId;

        public CosmosService(IConfiguration configuration)
        {
            

            string uri = configuration["CosmosUri"];
            string key = configuration["CosmosKey"];

            databaseId = configuration["CosmosDatabase"];
            containerId = configuration["CosmosContainer"];

            cosmosClient = new CosmosClient(uri, key);

        }

        
        // First call is made to this when chat page is loaded for left-hand nav.
        // Only retrieve the chat sessions, not chat messages
        public async Task<List<ChatSession>> GetChatSessionsListAsync()
        {

            if (chatContainer == null)
                chatContainer = await CreateContainerIfNotExistsAsync(databaseId, containerId);

            List<ChatSession> chatSessions = new();

            try { 
                //Get documents that are the chat sessions without the chat message documents.
                QueryDefinition query = new QueryDefinition("SELECT DISTINCT c.id, c.Type, c.ChatSessionId, c.ChatSessionName FROM c WHERE c.Type = @Type")
                    .WithParameter("@Type", "ChatSession");

                FeedIterator<ChatSession> results = chatContainer.GetItemQueryIterator<ChatSession>(query);

                while (results.HasMoreResults)
                {
                    FeedResponse<ChatSession> response = await results.ReadNextAsync();
                    foreach (ChatSession chatSession in response)
                    {
                        chatSessions.Add(chatSession);
                    }
                
                }
            }
            catch(CosmosException ce)
            {
                //if 404, first run, create a new default chat session document.
                if (ce.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ChatSession chatSession = new ChatSession();
                    await InsertChatSessionAsync(chatSession);
                    chatSessions.Add(chatSession);
                }

            }

            return chatSessions;

        }

        public async Task<ChatSession> InsertChatSessionAsync(ChatSession chatSession)
        {

            return await chatContainer.CreateItemAsync<ChatSession>(chatSession, new PartitionKey(chatSession.ChatSessionId));

        }

        public async Task<ChatSession> UpdateChatSessionAsync(ChatSession chatSession)
        {

            return await chatContainer.ReplaceItemAsync(item: chatSession, id: chatSession.Id, partitionKey: new PartitionKey(chatSession.ChatSessionId));

        }

        public async Task DeleteChatSessionAsync(string chatSessionId)
        {
            //Retrieve the chat session and all the chat message items for a chat session
            QueryDefinition query = new QueryDefinition("SELECT c.id, c.ChatSessionId FROM c WHERE c.ChatSessionId = @ID")
                    .WithParameter("@ID", chatSessionId);


            FeedIterator<ItemResponse> results = chatContainer.GetItemQueryIterator<ItemResponse>(query);

            while (results.HasMoreResults)
            {
                FeedResponse<ItemResponse> response = await results.ReadNextAsync();
                foreach (ItemResponse responseItem in response)
                {
                    await chatContainer.DeleteItemAsync<ItemResponse>(id: responseItem.Id, partitionKey: new PartitionKey(responseItem.ChatSessionId));
                }

            }


        }

        private class ItemResponse
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "ChatSessionId")]
            public string ChatSessionId { get; set; }
        }

            public async Task<ChatMessage> InsertChatMessageAsync(ChatMessage chatMessage)
        {

            return await chatContainer.CreateItemAsync<ChatMessage>(chatMessage, new PartitionKey(chatMessage.ChatSessionId));
            
        }

        public async Task<List<ChatMessage>> GetChatSessionMessagesAsync(string chatSessionId)
        {

            //Get the chat messages for a chat session
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.ChatSessionId = @ChatSessionId AND c.Type = @Type")
                .WithParameter("@ChatSessionId", chatSessionId)
                .WithParameter("@Type", "ChatMessage");

            FeedIterator<ChatMessage> results = chatContainer.GetItemQueryIterator<ChatMessage>(query);

            List<ChatMessage> chatMessages= new List<ChatMessage>();
            
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

        public async Task<Container> CreateContainerIfNotExistsAsync(string databaseId, string containerId)
        {

            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId); //= cosmosClient.GetDatabase(databaseId); //

            ContainerProperties properties = new ContainerProperties();

            properties.Id = containerId;
            properties.PartitionKeyDefinitionVersion = PartitionKeyDefinitionVersion.V2;
            properties.PartitionKeyPath = "/ChatSessionId";

            properties.IndexingPolicy.Automatic = true;
            properties.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
            properties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            //properties.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/Response/?" });
            properties.IndexingPolicy.CompositeIndexes.Add(
                new Collection<CompositePath> { 
                    new CompositePath() { 
                        Path = "/ChatName", Order = CompositePathSortOrder.Ascending,}, 
                    new CompositePath() { 
                        Path = "/DateTime", Order = CompositePathSortOrder.Ascending 
                    } 
                });

            ThroughputProperties throughput = ThroughputProperties.CreateAutoscaleThroughput(1000);

            return await database.CreateContainerIfNotExistsAsync(properties, throughput);

            
        }
    }

}
