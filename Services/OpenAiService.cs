using Azure;
using Azure.AI.OpenAI;

namespace Cosmos.Chat.GPT.Services;

public class OpenAiService
{
    private readonly string _endpoint;
    private readonly string _key;
    private readonly string _deploymentName;
    private readonly int _maxTokens;
    private readonly OpenAIClient _client;

    public int MaxTokens
    {
        get => _maxTokens;
    }

    public OpenAiService(string endpoint, string key, string deploymentName, string maxTokens)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNullOrEmpty(deploymentName);
        ArgumentNullException.ThrowIfNullOrEmpty(maxTokens);

        _endpoint = endpoint;
        _key = key;
        _deploymentName = deploymentName;
        _maxTokens = Int32.TryParse(maxTokens, out _maxTokens) ? _maxTokens : 3000;

        OpenAIClient client = new(new Uri(_endpoint), new AzureKeyCredential(_key));

        _client = client ??
            throw new ArgumentException("Unable to connect to existing Azure OpenAI endpoint.");
    }

    public async Task<string> AskAsync(string chatSessionId, string prompt)
    {
        CompletionsOptions completionsOptions = new()
        {
            Prompt = { prompt },
            User = chatSessionId,
            MaxTokens = _maxTokens

            //Temperature = 1,
            //Model = "text-davinci-003",
            //FrequencyPenalty = 0,
            //PresencePenalty = 0
        };

        Response<Completions> completionsResponse = await _client.GetCompletionsAsync(_deploymentName, completionsOptions);

        return completionsResponse.Value.Choices[0].Text;
    }
}
