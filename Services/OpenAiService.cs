using Cosmos.Chat.GPT.Models;

namespace Cosmos.Chat.GPT.Services;

public class OpenAiService
{
    private readonly string _modelName = String.Empty;

    public OpenAiService(string endpoint, string key, string modelName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(modelName);
    }

    public async Task<(string completionText, int completionTokens)> GetChatCompletionAsync(string sessionId, string userPrompt)
    {
        await Task.Delay(millisecondsDelay: 500);
        return ("&ltRESPONSE&gt;", 0);
    }

    public async Task<string> SummarizeAsync(string sessionId, string conversationText)
    {
        await Task.Delay(millisecondsDelay: 500);
        return "&ltSUMMARY&gt;";
    }
}
