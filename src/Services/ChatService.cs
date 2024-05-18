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

        _maxConversationTokens = Int32.TryParse(maxConversationTokens, out _maxConversationTokens) ? _maxConversationTokens : 100;
        _cacheSimilarityScore = Double.TryParse(cacheSimilarityScore, out _cacheSimilarityScore) ? _cacheSimilarityScore : 0.99;
    }

    public async Task<Message> GetChatCompletionAsync(string? sessionId, string promptText)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        Message chatMessage = await CreateChatMessageAsync(sessionId, promptText);

        List<Message> messages = new List<Message>();
        messages.Add(chatMessage);
        (chatMessage.Completion, chatMessage.CompletionTokens) = await _openAiService.GetChatCompletionAsync(sessionId, messages);

        await UpdateSessionAndMessage(sessionId, chatMessage);

        return chatMessage;
    }

    private async Task<List<Message>> GetChatSessionContextWindow(string sessionId)
    {
        List<Message> allMessages = await _cosmosDbService.GetSessionMessagesAsync(sessionId);
        List<Message> contextWindow = new List<Message>();

        return contextWindow;
    }

    private async Task<(string cachePrompts, float[] cacheVectors, string cacheResponse)> CacheGetAsync(List<Message> contextWindow)
    {
        string prompts = string.Empty;
        float[] vectors = new float[0];
        string response = string.Empty;
        await Task.Delay(0);

        return (prompts, vectors, response);
    }

    public async Task<string> SummarizeChatSessionNameAsync(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        List<Message> messages = await _cosmosDbService.GetSessionMessagesAsync(sessionId);
        string conversationText = string.Join(" ", messages.Select(m => m.Prompt + " " + m.Completion));

        string completionText = await _openAiService.SummarizeAsync(sessionId, conversationText);

        await RenameChatSessionAsync(sessionId, completionText);
        return completionText;
    }

    public async Task<List<Session>> GetAllChatSessionsAsync()
    {
        return await _cosmosDbService.GetSessionsAsync();
    }

    public async Task<List<Message>> GetChatSessionMessagesAsync(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        return await _cosmosDbService.GetSessionMessagesAsync(sessionId); ;
    }

    public async Task<Session> CreateNewChatSessionAsync()
    {
        Session session = new();
        await _cosmosDbService.InsertSessionAsync(session);
        return session;
    }

    public async Task RenameChatSessionAsync(string? sessionId, string newChatSessionName)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        Session session = await _cosmosDbService.GetSessionAsync(sessionId);
        session.Name = newChatSessionName;
        await _cosmosDbService.UpdateSessionAsync(session);
    }

    public async Task DeleteChatSessionAsync(string? sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        await _cosmosDbService.DeleteSessionAndMessagesAsync(sessionId);
    }

    private async Task<Message> CreateChatMessageAsync(string sessionId, string promptText)
    {
        int promptTokens = GetTokens(promptText);
        Message chatMessage = new(sessionId, promptTokens, promptText, "");
        await _cosmosDbService.InsertMessageAsync(chatMessage);
        return chatMessage;
    }

    private async Task UpdateSessionAndMessage(string sessionId, Message chatMessage)
    {
        Session session = await _cosmosDbService.GetSessionAsync(sessionId);
        session.Tokens += chatMessage.PromptTokens + chatMessage.CompletionTokens;
        await _cosmosDbService.UpsertSessionBatchAsync(session, chatMessage);
    }

    private int GetTokens(string userPrompt)
    {
        Tokenizer _tokenizer = Tokenizer.CreateTiktokenForModel("gpt-3.5-turbo");
        return _tokenizer.CountTokens(userPrompt);
    }

    private async Task CachePutAsync(string cachePrompts, float[] cacheVectors, string generatedCompletion)
    {
        CacheItem cacheItem = new(cacheVectors, cachePrompts, generatedCompletion);
        await _cosmosDbService.CachePutAsync(cacheItem);
    }

    public async Task CacheClearAsync()
    {
        await _cosmosDbService.CacheClearAsync();
    }
}