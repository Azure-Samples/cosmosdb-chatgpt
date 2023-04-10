using Cosmos.Chat.Models;

namespace Cosmos.Chat.Services;

public class ChatService
{

    //All data is cached in chatSessions List object.
    private static List<ChatSession> chatSessions = new();

    private readonly CosmosService _cosmosDbService;
    private readonly OpenAiService _openAiService;
    private readonly int maxConversationLength;


    public ChatService(CosmosService cosmosService, OpenAiService openAiService)
    {
        _cosmosDbService = cosmosService;
        _openAiService = openAiService;

        maxConversationLength = openAiService.MaxTokens / 2;
    }

    // Returns list of chat session ids and names for left-hand nav to bind to (display ChatSessionName and ChatSessionId as hidden)
    public async Task<List<ChatSession>> GetAllChatSessionsAsync()
    {
        chatSessions = await _cosmosDbService.GetChatSessionsAsync();

        return chatSessions;
    }

    //Returns the chat messages to display on the main web page when the user selects a chat from the left-hand nav
    public async Task<List<ChatMessage>> GetChatSessionMessagesAsync(string chatSessionId)
    {

        List<ChatMessage> chatMessages = new();

        if (chatSessions.Count == 0)
        {
            return Enumerable.Empty<ChatMessage>().ToList();
        }

        int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

        if (chatSessions[index].Messages.Count == 0)
        {
            //Messages are not cached, go read from database
            chatMessages = await _cosmosDbService.GetChatSessionMessagesAsync(chatSessionId);

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
        ChatSession chatSession = new();

        chatSessions.Add(chatSession);

        await _cosmosDbService.InsertChatSessionAsync(chatSession);

    }

    //User Inputs a chat from "New Chat" to user defined
    public async Task RenameChatSessionAsync(string chatSessionId, string newChatSessionName)
    {

        int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

        chatSessions[index].ChatSessionName = newChatSessionName;

        await _cosmosDbService.UpdateChatSessionAsync(chatSessions[index]);

    }

    //User deletes a chat session
    public async Task DeleteChatSessionAsync(string chatSessionId)
    {
        int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

        chatSessions.RemoveAt(index);

        await _cosmosDbService.DeleteChatSessionAsync(chatSessionId);


    }

    //User prompt to ask _openAiService a question
    public async Task<string> AskOpenAiAsync(string chatSessionId, string prompt)
    {
        await AddPromptMessageAsync(chatSessionId, prompt);

        string conversation = GetChatSessionConversation(chatSessionId);

        string response = await _openAiService.AskAsync(chatSessionId, conversation);

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


            foreach (ChatMessage chatMessage in chatMessages)
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
        string response = await _openAiService.AskAsync(chatSessionId, prompt);

        await RenameChatSessionAsync(chatSessionId, response);

        return response;

    }

    // Add human prompt to the chat session message list object and insert into _cosmosDbService.
    private async Task AddPromptMessageAsync(string chatSessionId, string text)
    {
        ChatMessage chatMessage = new(chatSessionId, "Human", text);

        int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

        chatSessions[index].AddMessage(chatMessage);

        await _cosmosDbService.InsertChatMessageAsync(chatMessage);

    }

    // Add _openAiService response to the chat session message list object and insert into _cosmosDbService.
    private async Task AddResponseMessageAsync(string chatSessionId, string text)
    {
        ChatMessage chatMessage = new(chatSessionId, "AI", text);

        int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

        chatSessions[index].AddMessage(chatMessage);

        await _cosmosDbService.InsertChatMessageAsync(chatMessage);

    }

}
