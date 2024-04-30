using Cosmos.Chat.GPT.Models;
using Microsoft.ML.Tokenizers;

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
    private readonly double _cacheSimilarityScore;

    public ChatService(CosmosDbService cosmosDbService, OpenAiService openAiService, string maxConversationTokens, string cacheSimilarityScore)
    {
        _cosmosDbService = cosmosDbService;
        _openAiService = openAiService;

        _maxConversationTokens = Int32.TryParse(maxConversationTokens, out _maxConversationTokens) ? _maxConversationTokens : 4000;
        _cacheSimilarityScore = Double.TryParse(cacheSimilarityScore, out _cacheSimilarityScore) ? _cacheSimilarityScore : 0.99;
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

        return await _cosmosDbService.GetSessionMessagesAsync(sessionId); ;
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
    /// Rename the Chat Session from "New Chat" to the summary provided by OpenAI
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
    /// Get a completion from Azure OpenAi Service
    /// </summary>
    public async Task<Message> GetChatCompletionAsync(string? sessionId, string promptText)
    {

        ArgumentNullException.ThrowIfNull(sessionId);

        //Create a message object for the new User Prompt, also calculates the tokens for the prompt
        Message chatMessage = await CreateChatMessageAsync(sessionId, promptText);

        //Grab conversation history up to the maximum configured tokens
        List<Message> conversation = await GetChatSessionContextWindow(sessionId);

        //// Lab Exercise
        //Perform a cache search to see if this prompt has already been used in the same context window as this conversation
        (string cachePrompts, float[] cacheVectors, string cacheResponse) = await CacheGetAsync(conversation);

        //Cache hit, return the cached completion
        if (!string.IsNullOrEmpty(cacheResponse))
        {
            chatMessage.Completion = cacheResponse;
            chatMessage.Completion += " (cached response)";
            chatMessage.CompletionTokens = 0;
        }
        else  //Cache miss, send to OpenAI to generate a completion
        { 
            //Serialize the conversation to a string to send to OpenAI
            string conversationString = string.Join(Environment.NewLine, conversation.Select(m => m.Prompt + " " + m.Completion));

            //Generate a completion and tokens used from the user prompt and conversation
            (chatMessage.Completion, chatMessage.CompletionTokens) = await _openAiService.GetChatCompletionAsync(sessionId, conversationString);

            //Cache the prompts in the current context window and their vectors with the generated completion
            await CachePutAsync(cachePrompts, cacheVectors, chatMessage.Completion);
        }

        //Persist the prompt/completion, update the session tokens
        await UpdateSessionAndMessage(sessionId, chatMessage);

        return chatMessage;
    }

    /// <summary>
    /// Get the context window for this conversation. This is used in cache search as well as generating completions
    /// </summary>
    private async Task<List<Message>> GetChatSessionContextWindow(string sessionId)
    {

        int? tokensUsed = 0;

        //List<string> conversationBuilder = new List<string>();
        //int index = _sessions.FindIndex(s => s.SessionId == sessionId);
        //List<Message> messages = _sessions[index].Messages;

        List<Message> allMessages = await _cosmosDbService.GetSessionMessagesAsync(sessionId);
        List<Message> contextWindow = new List<Message>();

        //Start at the end of the list and work backwards
        //This includes the latest user prompt which is already cached
        for (int i = allMessages.Count - 1; i >= 0; i--)
        {
            tokensUsed += allMessages[i].PromptTokens + allMessages[i].CompletionTokens;

            if (tokensUsed > _maxConversationTokens)
                break;

            contextWindow.Add(allMessages[i]);
        }

        //Invert the chat messages to put back into chronological order 
        contextWindow = contextWindow.Reverse<Message>().ToList();

        return contextWindow;

    }

    /// <summary>
    /// Have OpenAI summarize the conversation based upon the prompt and completion text in the session
    /// </summary>
    public async Task<string> SummarizeChatSessionNameAsync(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        //Get the messages for the session
        List<Message> messages = await _cosmosDbService.GetSessionMessagesAsync(sessionId);

        //Create a conversation string from the messages
        string conversationText = string.Join(Environment.NewLine, messages.Select(m => m.Prompt + " " + m.Completion));

        string completionText = await _openAiService.SummarizeAsync(sessionId, conversationText);

        await RenameChatSessionAsync(sessionId, completionText);

        return completionText;
    }

    /// <summary>
    /// Calculate token count for prompt text. Add user prompt to the chat session message list object
    /// </summary>
    private async Task<Message> CreateChatMessageAsync(string sessionId, string promptText)
    {

        //Calculate tokens for the user prompt message.
        int promptTokens = GetTokens(promptText);

        //Create a new message object. This gets used later for building the conversation history.
        Message chatMessage = new(sessionId, promptTokens, promptText, "");

        await _cosmosDbService.InsertMessageAsync(chatMessage);

        return chatMessage;
    }

    /// <summary>
    /// Update session with user prompt and completion tokens and update the cache
    /// </summary>
    private async Task UpdateSessionAndMessage(string sessionId, Message chatMessage)
    {

        //Update the tokens used in the session
        Session session = await _cosmosDbService.GetSessionAsync(sessionId);
        session.Tokens += chatMessage.PromptTokens + chatMessage.CompletionTokens;

        //Insert new message and Update session in a transaction
        await _cosmosDbService.UpsertSessionBatchAsync(session, chatMessage);

    }

    /// <summary>
    /// Calculate the number of tokens from the user prompt
    /// </summary>
    private int GetTokens(string userPrompt)
    {
        Tokenizer _tokenizer = Tokenizer.CreateTiktokenForModel("gpt-3.5-turbo");

        //Create a new instance of SharpToken
        //var encoding = GptEncoding.GetEncodingForModel("gpt-3.5-turbo");

        //Get count of vectors on user prompt (return)
        //return encoding.Encode(userPrompt).Count;

        return _tokenizer.CountTokens(userPrompt);

    }


    /// <summary>
    /// Consult the semantic cache for similar vectors for the same context window for this conversation
    /// </summary>
    private async Task<(string cachePrompts, float[] cacheVectors, string cacheResponse)> CacheGetAsync(List<Message> contextWindow)
    {
        //Grab the user prompts for the context window
        string prompts = string.Join(Environment.NewLine, contextWindow.Select(m => m.Prompt));

        //Get the embeddings for the user prompts
        float[] vectors = await _openAiService.GetEmbeddingsAsync(prompts);

        //Check the cache for similar vectors
        string response = await _cosmosDbService.CacheGetAsync(vectors, 0.99);

        return (prompts, vectors, response);
    }

    private async Task CachePutAsync(string cachePrompts, float[] cacheVectors, string generatedCompletion)
    {
        CacheItem cacheItem = new(cacheVectors, cachePrompts, generatedCompletion);

        //Put the prompts, vectors and completion into the cache
        await _cosmosDbService.CachePutAsync(cacheItem);
    }
}