using Azure;
using Azure.AI.OpenAI;

namespace cosmosdb_chatgpt.Services
{
    public class OpenAiService
    {

        private readonly OpenAIClient client;
        private readonly string deployment;
        private readonly int maxTokens;

        public OpenAiService(IConfiguration configuration) 
        {
            

            string openAiUri = configuration["OpenAiUri"];
            string openAiKey = configuration["OpenAiKey"];
            deployment = configuration["OpenAiDeployment"];
            maxTokens = int.Parse(configuration["OpenAiMaxTokens"]);

            client = new(new Uri(openAiUri), new AzureKeyCredential(openAiKey));

        }

        public async Task<string> AskAsync(string chatSessionId, string prompt)
        {


            CompletionsOptions completionsOptions = new CompletionsOptions()
            {
                Prompt = { prompt },
                User = chatSessionId,
                MaxTokens = maxTokens
                
                //Temperature = 1,
                //Model = "text-davinci-003",
                //FrequencyPenalty = 0,
                //PresencePenalty = 0
            };


            Response<Completions> completionsResponse = await client.GetCompletionsAsync(deployment, completionsOptions);

            return completionsResponse.Value.Choices[0].Text;

        }

    }

}
