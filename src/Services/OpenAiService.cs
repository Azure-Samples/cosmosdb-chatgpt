using Azure;
using Azure.AI.OpenAI;
using Cosmos.Chat.GPT.Models;

namespace Cosmos.Chat.GPT.Services;

public class OpenAiService
{
    private readonly string _completionDeploymentName = String.Empty;
    private readonly string _embeddingDeploymentName = String.Empty;
    private readonly OpenAIClient _client;

    private readonly string _systemPrompt = @"
        You are an AI assistant that helps people find information.
        Provide concise answers that are polite and professional." + Environment.NewLine;

    private readonly string _summarizePrompt = @"
        Summarize this prompt in one or two words to use as a label in a button on a web page.
        Do not use any punctuation." + Environment.NewLine;

    public OpenAiService(string endpoint, string key, string completionDeploymentName, string embeddingDeploymentName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNullOrEmpty(completionDeploymentName);
        ArgumentNullException.ThrowIfNullOrEmpty(embeddingDeploymentName);
        _completionDeploymentName = completionDeploymentName;
        _embeddingDeploymentName = embeddingDeploymentName;
        _client = new(new Uri(endpoint), new AzureKeyCredential(key));
    }

    public async Task<(string completion, int tokens)> GetChatCompletionAsync(string sessionId, List<Message> conversation)
    {
        await Task.Delay(0);
        string completion = "Place holder response";
        int tokens = 0;

        return (completion, tokens);
    }

    public async Task<string> SummarizeAsync(string sessionId, string conversationText)
    {
        ChatRequestSystemMessage systemMessage = new(_summarizePrompt);
        ChatRequestUserMessage userMessage = new(conversationText);
        ChatCompletionsOptions options = new()
        {
            DeploymentName = _completionDeploymentName,
            Messages = {
                systemMessage,
                userMessage
            },
            User = sessionId,
            MaxTokens = 200,
            Temperature = 0.0f,
            NucleusSamplingFactor = 1.0f
        };

        Response<ChatCompletions> completionsResponse = await _client.GetChatCompletionsAsync(options);
        ChatCompletions completions = completionsResponse.Value;
        string completionText = completions.Choices[0].Message.Content;
        return completionText;
    }

    public async Task<float[]> GetEmbeddingsAsync(string input)
    {
        float[] embedding = new float[0];
        EmbeddingsOptions options = new EmbeddingsOptions(_embeddingDeploymentName, new List<string> { input });
        var response = await _client.GetEmbeddingsAsync(options);
        Embeddings embeddings = response.Value;
        embedding = embeddings.Data[0].Embedding.ToArray();
        return embedding;
    }
}
