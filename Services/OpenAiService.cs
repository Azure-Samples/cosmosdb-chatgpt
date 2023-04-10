using Azure;
using Azure.AI.OpenAI;

namespace Cosmos.Chat.Services
{
    public class OpenAiService
    {
        private readonly OpenAIClient _client;
        private readonly string _deploymentName;
        private readonly int _maxTokens;

        public int MaxTokens
        {
            get => _maxTokens;
        }

        public OpenAiService(string endpoint, string key, string deploymentName, string maxTokens)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(deploymentName);
            ArgumentNullException.ThrowIfNull(maxTokens);

            _deploymentName = deploymentName;
            _maxTokens = Int32.TryParse(maxTokens, out _maxTokens) ? _maxTokens : 3000;

            OpenAIClient client = new(new Uri(endpoint), new AzureKeyCredential(key));

            _client = client ??
                throw new ArgumentException("Unable to connect to existing Azure OpenAI endpoint.");
        }

        public async Task<string> AskAsync(string chatSessionId, string prompt)
        {
            CompletionsOptions completionsOptions = new CompletionsOptions()
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
}
