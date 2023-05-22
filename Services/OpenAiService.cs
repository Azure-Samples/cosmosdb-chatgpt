using Cosmos.Chat.GPT.Models;
using Azure;
using Azure.AI.OpenAI;

namespace Cosmos.Chat.GPT.Services;

public class OpenAiService
{
    private readonly string _modelName = String.Empty;

    private readonly OpenAIClient _client = default!;

    public OpenAiService(string endpoint, string key, string modelName)
    {
    }

    public async Task<(string response, int promptTokens, int responseTokens)> GetChatCompletionAsync(string sessionId, string userPrompt)
    {
        await Task.Delay(millisecondsDelay: 500);
        return ("&ltRESPONSE&gt;", 0, 0);
    }

    public async Task<string> SummarizeAsync(string sessionId, string userPrompt)
    {
        await Task.Delay(millisecondsDelay: 500);
        return "&ltSUMMARY&gt;";
    }
}
