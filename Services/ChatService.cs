using Cosmos.Chat.GPT.Constants;
using Cosmos.Chat.GPT.Models;

namespace Cosmos.Chat.GPT.Services;

public class ChatService
{
    /// <summary>
    /// All data is cached in the _sessions List object.
    /// </summary>
    private static List<Session> _sessions = new();

    private readonly CosmosDbService _cosmosDbService;
    private readonly OpenAiService _openAiService;
    private readonly int _maxConversationTokens;

    public ChatService(CosmosDbService cosmosDbService, OpenAiService openAiService, string maxConversationTokens)
    {
        _cosmosDbService = cosmosDbService;
        _openAiService = openAiService;
        
        _maxConversationTokens = Int32.TryParse(maxConversationTokens, out _maxConversationTokens) ? _maxConversationTokens : 4000;
    }

    /// <summary>
    /// Returns list of chat session ids and names for left-hand nav to bind to (display Name and ChatSessionId as hidden)
    /// </summary>
    public async Task<List<Session>> GetAllChatSessionsAsync()
    {
        return _sessions = await _cosmosDbService.GetSessionsAsync();
    }

    /// <summary>
    /// Returns the chat messages to display on the main web page when the user selects a chat from the left-hand nav
    /// </summary>
    public async Task<List<Message>> GetChatSessionMessagesAsync(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        List<Message> chatMessages = new();

        if (_sessions.Count == 0)
        {
            return Enumerable.Empty<Message>().ToList();
        }

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        if (_sessions[index].Messages.Count == 0)
        {
            // Messages are not cached, go read from database
            chatMessages = await _cosmosDbService.GetSessionMessagesAsync(sessionId);

            // Cache results
            _sessions[index].Messages = chatMessages;
        }
        else
        {
            // Load from cache
            chatMessages = _sessions[index].Messages;
        }

        return chatMessages;
    }

    /// <summary>
    /// User creates a new Chat Session.
    /// </summary>
    public async Task CreateNewChatSessionAsync()
    {
        Session session = new();

        _sessions.Add(session);

        await _cosmosDbService.InsertSessionAsync(session);

    }

    /// <summary>
    /// Rename the Chat Ssssion from "New Chat" to the summary provided by OpenAI
    /// </summary>
    public async Task RenameChatSessionAsync(string? sessionId, string newChatSessionName)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        _sessions[index].Name = newChatSessionName;

        await _cosmosDbService.UpdateSessionAsync(_sessions[index]);
    }

    /// <summary>
    /// User deletes a chat session
    /// </summary>
    public async Task DeleteChatSessionAsync(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        _sessions.RemoveAt(index);

        await _cosmosDbService.DeleteSessionAndMessagesAsync(sessionId);
    }

    /// <summary>
    /// Get a completion from _openAiService
    /// </summary>
    public async Task<string> GetChatCompletionAsync(string? sessionId, string prompt)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        Message promptMessage = await AddPromptMessageAsync(sessionId, prompt);

        string conversation = GetChatSessionConversation(sessionId);

        (string response, int promptTokens, int responseTokens) = await _openAiService.GetChatCompletionAsync(sessionId, conversation);

        await AddPromptCompletionMessagesAsync(sessionId, promptTokens, responseTokens, promptMessage, response);

        return response;
    }

    /// <summary>
    /// Get current conversation from newest to oldest up to max conversation tokens and add to the prompt
    /// </summary>
    private string GetChatSessionConversation(string sessionId)
    {

        int? tokensUsed = 0;

        List<string> conversationBuilder = new List<string>();
        
        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        List<Message> messages = _sessions[index].Messages;

        //Start at the end of the list and work backwards
        for(int i = messages.Count - 1; i >= 0; i--) 
        {             
            tokensUsed += messages[i].Tokens is null ? 0 : messages[i].Tokens;

            if(tokensUsed > _maxConversationTokens)
                break;
            
            conversationBuilder.Add(messages[i].Text);
        }

        //Invert the chat messages to put back into chronological order and output as string.        
        string conversation = string.Join(Environment.NewLine, conversationBuilder.Reverse<string>());

        return conversation;

    }

    public async Task<string> SummarizeChatSessionNameAsync(string? sessionId, string prompt)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        string response = await _openAiService.SummarizeAsync(sessionId, prompt);

        await RenameChatSessionAsync(sessionId, response);

        return response;
    }

    /// <summary>
    /// Add user prompt to the chat session message list object and insert into the data service.
    /// </summary>
    private async Task<Message> AddPromptMessageAsync(string sessionId, string promptText)
    {
        Message promptMessage = new(sessionId, nameof(Participants.User), default, promptText);

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);

        _sessions[index].AddMessage(promptMessage);

        return await _cosmosDbService.InsertMessageAsync(promptMessage);
    }

    /// <summary>
    /// Add user prompt and AI assistance response to the chat session message list object and insert into the data service as a transaction.
    /// </summary>
    private async Task AddPromptCompletionMessagesAsync(string sessionId, int promptTokens, int completionTokens, Message promptMessage, string completionText)
    {

        int index = _sessions.FindIndex(s => s.SessionId == sessionId);
        
        //Create completion message, add to the cache
        Message completionMessage = new(sessionId, nameof(Participants.Assistant), completionTokens, completionText);
        _sessions[index].AddMessage(completionMessage);

        
        //Update prompt message with tokens used and insert into the cache
        Message updatedPromptMessage = promptMessage with { Tokens = promptTokens };
        _sessions[index].UpdateMessage(updatedPromptMessage);


        //Update session with tokens users and udate the cache
        _sessions[index].TokensUsed += updatedPromptMessage.Tokens;
        _sessions[index].TokensUsed += completionMessage.Tokens;


        await _cosmosDbService.UpsertSessionBatchAsync(updatedPromptMessage, completionMessage, _sessions[index]);
        
    }
}