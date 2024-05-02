using Cosmos.Chat.GPT.Models;
using Microsoft.ML.Tokenizers;

namespace Cosmos.Chat.GPT.Services;

public class ChatService
{

    private readonly CosmosDbService _cosmosDbService;
    private readonly OpenAiService _openAiService;
    private readonly SemanticKernelService _semanticKernelService;
    private readonly int _maxConversationTokens;
    private readonly double _cacheSimilarityScore;

    public ChatService(CosmosDbService cosmosDbService, OpenAiService openAiService, SemanticKernelService semanticKernelService, string maxConversationTokens, string cacheSimilarityScore)
    {
        _cosmosDbService = cosmosDbService;
        _openAiService = openAiService;
        _semanticKernelService = semanticKernelService;

        _maxConversationTokens = Int32.TryParse(maxConversationTokens, out _maxConversationTokens) ? _maxConversationTokens : 4000;
        _cacheSimilarityScore = Double.TryParse(cacheSimilarityScore, out _cacheSimilarityScore) ? _cacheSimilarityScore : 0.99;
    }

    /// <summary>
    /// Returns list of chat session ids and names for left-hand nav to bind to (display Name and ChatSessionId as hidden)
    /// </summary>
    public async Task<List<Session>> GetAllChatSessionsAsync()
    {
        return await _cosmosDbService.GetSessionsAsync();
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

        await _cosmosDbService.InsertSessionAsync(session);

    }

    /// <summary>
    /// Rename the Chat Session from "New Chat" to the summary provided by OpenAI
    /// </summary>
    public async Task RenameChatSessionAsync(string? sessionId, string newChatSessionName)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        Session session = await _cosmosDbService.GetSessionAsync(sessionId);

        session.Name = newChatSessionName;

        await _cosmosDbService.UpdateSessionAsync(session);
    }

    /// <summary>
    /// User deletes a chat session
    /// </summary>
    public async Task DeleteChatSessionAsync(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        await _cosmosDbService.DeleteSessionAndMessagesAsync(sessionId);
    }


    public async Task<Message> GetChatCompletionAsyncV1(string? sessionId, string promptText)
    {

        ArgumentNullException.ThrowIfNull(sessionId);

        //Create a message object for the new User Prompt and calculate the tokens for the prompt
        Message chatMessage = await CreateChatMessageAsync(sessionId, promptText);

        List<Message> messages = new List<Message>();
        messages.Add(chatMessage);

        //Generate a completion and tokens used from current context window which includes the latest user prompt
        (chatMessage.Completion, chatMessage.CompletionTokens) = await _openAiService.GetChatCompletionAsync(sessionId, messages);

        //Persist the prompt/completion, update the session tokens
        await UpdateSessionAndMessage(sessionId, chatMessage);

        return chatMessage;
    }

    public async Task<Message> GetChatCompletionAsyncV2(string? sessionId, string promptText)
    {

        ArgumentNullException.ThrowIfNull(sessionId);

        //Create a message object for the new User Prompt and calculate the tokens for the prompt
        Message chatMessage = await CreateChatMessageAsync(sessionId, promptText);

        //Exercise #2, implement the GetChatSessionContextWindow function and add a call to it here.
        //Grab context window from the conversation history up to the maximum configured tokens
        List<Message> contextWindow = await GetChatSessionContextWindow(sessionId);

        //Generate a completion and tokens used from current context window which includes the latest user prompt
        (chatMessage.Completion, chatMessage.CompletionTokens) = await _openAiService.GetChatCompletionAsync(sessionId, contextWindow);

        //Persist the prompt/completion, update the session tokens
        await UpdateSessionAndMessage(sessionId, chatMessage);

        return chatMessage;
    }

    public async Task<Message> GetChatCompletionAsyncV3(string? sessionId, string promptText)
    {

        ArgumentNullException.ThrowIfNull(sessionId);

        //Create a message object for the new User Prompt and calculate the tokens for the prompt
        Message chatMessage = await CreateChatMessageAsync(sessionId, promptText);

        //Grab context window from the conversation history up to the maximum configured tokens
        List<Message> contextWindow = await GetChatSessionContextWindow(sessionId);

        //Exercise #3, implement the CacheGetAsync and PutCache async function and add the if/else statement
        //Perform a cache search to see if this prompt has already been used in the same context window as this conversation
        (string cachePrompts, float[] cacheVectors, string cacheResponse) = await CacheGetAsync(contextWindow);

        //Cache hit, return the cached completion
        if (!string.IsNullOrEmpty(cacheResponse))
        {
            chatMessage.Completion = cacheResponse;
            chatMessage.Completion += " (cached response)";
            chatMessage.CompletionTokens = 0;

            //Persist the prompt/completion, update the session tokens
            await UpdateSessionAndMessage(sessionId, chatMessage);

            return chatMessage;
        }
        else  //Cache miss, send to OpenAI to generate a completion
        {

            //Generate a completion and tokens used from current context window which includes the latest user prompt
            (chatMessage.Completion, chatMessage.CompletionTokens) = await _openAiService.GetChatCompletionAsync(sessionId, contextWindow);

            //Cache the prompts in the current context window and their vectors with the generated completion
            await CachePutAsync(cachePrompts, cacheVectors, chatMessage.Completion);
        }


        //Persist the prompt/completion, update the session tokens
        await UpdateSessionAndMessage(sessionId, chatMessage);

        return chatMessage;
    }

    public async Task<Message> GetChatCompletionAsyncV4(string? sessionId, string promptText)
    {

        ArgumentNullException.ThrowIfNull(sessionId);

        //Create a message object for the new User Prompt and calculate the tokens for the prompt
        Message chatMessage = await CreateChatMessageAsync(sessionId, promptText);

        //Grab context window from the conversation history up to the maximum configured tokens
        List<Message> contextWindow = await GetChatSessionContextWindow(sessionId);

        //Exercise #4 , imlement the Semantic Kernel functions
        //replace Embeddings call in CacheGetAsync
        //Perform a cache search to see if this prompt has already been used in the same context window as this conversation
        (string cachePrompts, float[] cacheVectors, string cacheResponse) = await CacheGetAsync(contextWindow);

        //Cache hit, return the cached completion
        if (!string.IsNullOrEmpty(cacheResponse))
        {
            chatMessage.Completion = cacheResponse;
            chatMessage.Completion += " (cached response)";
            chatMessage.CompletionTokens = 0;

            //Persist the prompt/completion, update the session tokens
            await UpdateSessionAndMessage(sessionId, chatMessage);

            return chatMessage;
        }
        else  //Cache miss, send to OpenAI to generate a completion
        {

            //Exercise #4 , imlement the Semantic Kernel function
            //Generate a completion and tokens used from current context window which includes the latest user prompt
            (chatMessage.Completion, chatMessage.CompletionTokens) = await _semanticKernelService.GetChatCompletionAsync(sessionId, contextWindow);

            //Cache the prompts in the current context window and their vectors with the generated completion
            await CachePutAsync(cachePrompts, cacheVectors, chatMessage.Completion);
        }


        //Persist the prompt/completion, update the session tokens
        await UpdateSessionAndMessage(sessionId, chatMessage);

        return chatMessage;
    }

    /// <summary>
    /// Get a completion for a user prompt from Azure OpenAi Service
    /// </summary>
    public async Task<Message> GetChatCompletionAsync(string? sessionId, string promptText)
    {

        ArgumentNullException.ThrowIfNull(sessionId);

        //Create a message object for the new User Prompt and calculate the tokens for the prompt
        Message chatMessage = await CreateChatMessageAsync(sessionId, promptText);

        //Grab context window from the conversation history up to the maximum configured tokens
        List<Message> contextWindow = await GetChatSessionContextWindow(sessionId);

        //Perform a cache search to see if this prompt has already been used in the same context window as this conversation
        (string cachePrompts, float[] cacheVectors, string cacheResponse) = await CacheGetAsync(contextWindow);

        //Cache hit, return the cached completion
        if (!string.IsNullOrEmpty(cacheResponse))
        {
            chatMessage.Completion = cacheResponse;
            chatMessage.Completion += " (cached response)";
            chatMessage.CompletionTokens = 0;

            //Persist the prompt/completion, update the session tokens
            await UpdateSessionAndMessage(sessionId, chatMessage);

            return chatMessage;
        }
        else  //Cache miss, send to OpenAI to generate a completion
        {

            //Generate a completion and tokens used from current context window which includes the latest user prompt
            //(chatMessage.Completion, chatMessage.CompletionTokens) = await _openAiService.GetChatCompletionAsync(sessionId, contextWindow);
            (chatMessage.Completion, chatMessage.CompletionTokens) = await _semanticKernelService.GetChatCompletionAsync(sessionId, contextWindow);

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

    public async Task<string> SummarizeChatSessionNameAsyncV4(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);


        //Get the messages for the session
        List<Message> messages = await _cosmosDbService.GetSessionMessagesAsync(sessionId);

        //Create a conversation string from the messages
        string conversationText = string.Join(" ", messages.Select(m => m.Prompt + " " + m.Completion));

        //Exercise 4, implement the Semantic Kernel function
        string completionText = await _semanticKernelService.SummarizeConversationAsync(conversationText);

        await RenameChatSessionAsync(sessionId, completionText);

        return completionText;
    }

    /// <summary>
    /// Use OpenAI to summarize the conversation to give it a relevant name on the web page
    /// </summary>
    public async Task<string> SummarizeChatSessionNameAsync(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);


        //Get the messages for the session
        List<Message> messages = await _cosmosDbService.GetSessionMessagesAsync(sessionId);

        //Create a conversation string from the messages
        string conversationText = string.Join(" ", messages.Select(m => m.Prompt + " " + m.Completion));

        //Send to OpenAI to summarize the conversation
        //string completionText = await _openAiService.SummarizeAsync(sessionId, conversationText);
        string completionText = await _semanticKernelService.SummarizeConversationAsync(conversationText);

        await RenameChatSessionAsync(sessionId, completionText);

        return completionText;
    }

    /// <summary>
    /// Add user prompt to a new chat session message object, calculate token count for prompt text.
    /// </summary>
    private async Task<Message> CreateChatMessageAsync(string sessionId, string promptText)
    {

        //Calculate tokens for the user prompt message.
        int promptTokens = GetTokens(promptText);

        //Create a new message object.
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

        return _tokenizer.CountTokens(userPrompt);

    }

    private async Task<(string cachePrompts, float[] cacheVectors, string cacheResponse)> CacheGetAsyncV4(List<Message> contextWindow)
    {
        //Grab the user prompts for the context window
        string prompts = string.Join(Environment.NewLine, contextWindow.Select(m => m.Prompt));

        //Get the embeddings for the user prompts
        float[] vectors = await _semanticKernelService.GetEmbeddingsAsync(prompts);

        //Check the cache for similar vectors
        string response = await _cosmosDbService.CacheGetAsync(vectors, _cacheSimilarityScore);

        return (prompts, vectors, response);
    }
    /// <summary>
    /// Query the semantic cache with user prompt vectors for the current context window in this conversation
    /// </summary>
    private async Task<(string cachePrompts, float[] cacheVectors, string cacheResponse)> CacheGetAsync(List<Message> contextWindow)
    {
        //Grab the user prompts for the context window
        string prompts = string.Join(Environment.NewLine, contextWindow.Select(m => m.Prompt));

        //Get the embeddings for the user prompts
        float[] vectors = await _openAiService.GetEmbeddingsAsync(prompts);
        //float[] vectors = await _semanticKernelService.GetEmbeddingsAsync(prompts);

        //Check the cache for similar vectors
        string response = await _cosmosDbService.CacheGetAsync(vectors, _cacheSimilarityScore);

        return (prompts, vectors, response);
    }

    /// <summary>
    /// Cache the last generated completion with user prompt vectors for the current context window in this conversation
    /// </summary>
    private async Task CachePutAsync(string cachePrompts, float[] cacheVectors, string generatedCompletion)
    {
        //Include the user prompts text to view. They are not used in the cache search.
        CacheItem cacheItem = new(cacheVectors, cachePrompts, generatedCompletion);

        //Put the prompts, vectors and completion into the cache
        await _cosmosDbService.CachePutAsync(cacheItem);
    }
}