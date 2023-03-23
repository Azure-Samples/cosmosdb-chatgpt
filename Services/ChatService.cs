namespace cosmosdb_chatgpt.Services
{
    public class ChatService 
    {

        //All data is cached in chatSessions List object.
        private static List<ChatSession> chatSessions;

        private readonly CosmosService cosmos;
        private readonly OpenAiService openAi;
        private readonly int maxConversationLength;


        public ChatService(IConfiguration configuration)
        {
            maxConversationLength = int.Parse(configuration["OpenAiMaxTokens"]) / 2;

            cosmos = new CosmosService(configuration);
            openAi = new OpenAiService(configuration);
            
        }

        
        // Returns list of chat session ids and names for left-hand nav to bind to (display ChatSessionName and ChatSessionId as hidden)
        public async Task<List<ChatSession>> GetAllChatSessionsAsync()
        {
            chatSessions = await cosmos.GetChatSessionsAsync();
           
            return chatSessions;
        }

        //Returns the chat messages to display on the main web page when the user selects a chat from the left-hand nav
        public async Task<List<ChatMessage>> GetChatSessionMessagesAsync(string chatSessionId)
        {

            List<ChatMessage> chatMessages = new List<ChatMessage>();

            if (chatSessions.Count == 0) return null;

            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);
            
            if (chatSessions[index].Messages.Count == 0)
            { 
                //Messages are not cached, go read from database
                chatMessages = await cosmos.GetChatSessionMessagesAsync(chatSessionId);

                //cache results
                 chatSessions[index].Messages = chatMessages;

            }
            else
            {
                //load from cache
                chatMessages = chatSessions[index].Messages;
            }
            return chatMessages;

        }

        //User creates a new Chat Session
        public async Task CreateNewChatSessionAsync()
        {
            ChatSession chatSession = new ChatSession();

            chatSessions.Add(chatSession);
            
            await cosmos.InsertChatSessionAsync(chatSession);
                       
        }

        //User Inputs a chat from "New Chat" to user defined
        public async Task RenameChatSessionAsync(string chatSessionId, string newChatSessionName)
        {
            
            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

            chatSessions[index].ChatSessionName = newChatSessionName;

            await cosmos.UpdateChatSessionAsync(chatSessions[index]);

        }

        //User deletes a chat session
        public async Task DeleteChatSessionAsync(string chatSessionId)
        {
            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

            chatSessions.RemoveAt(index);

            await cosmos.DeleteChatSessionAsync(chatSessionId);


        }

        //User prompt to ask OpenAI a question
        public async Task<string> AskOpenAiAsync(string chatSessionId, string prompt)
        {
            await AddPromptMessageAsync(chatSessionId, prompt);

            string conversation = GetChatSessionConversation(chatSessionId);

            string response = await openAi.AskAsync(chatSessionId, conversation);

            await AddResponseMessageAsync(chatSessionId, response);

            return response;

        }

        private string GetChatSessionConversation(string chatSessionId)
        {
            string conversation = "";

            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

            if (chatSessions[index].Messages.Count > 0)
            {
                List<ChatMessage> chatMessages = chatSessions[index].Messages;

                
                foreach(ChatMessage chatMessage in chatMessages)
                {

                    conversation += chatMessage.Text + "\n";
                    
                }

                if (conversation.Length > maxConversationLength)
                    conversation = conversation.Substring(conversation.Length - maxConversationLength, maxConversationLength);

            }

            return conversation;
        }

        public async Task<string> SummarizeChatSessionNameAsync(string chatSessionId, string prompt)
        {
            prompt += "\n\n Summarize this prompt in one or two words to use as a label in a button on a web page";
            string response = await openAi.AskAsync(chatSessionId, prompt);

            await RenameChatSessionAsync(chatSessionId, response);

            return response;

        }

        // Add human prompt to the chat session message list object and insert into Cosmos.
        private async Task AddPromptMessageAsync(string chatSessionId, string text)
        {
            ChatMessage chatMessage = new ChatMessage(chatSessionId, "Human", text);

            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

            chatSessions[index].AddMessage(chatMessage);

            await cosmos.InsertChatMessageAsync(chatMessage);

        }

        // Add OpenAI response to the chat session message list object and insert into Cosmos.
        private async Task AddResponseMessageAsync(string chatSessionId, string text)
        {
            ChatMessage chatMessage = new ChatMessage(chatSessionId, "AI", text);

            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

            chatSessions[index].AddMessage(chatMessage);

            await cosmos.InsertChatMessageAsync(chatMessage);

        }

    }
}
