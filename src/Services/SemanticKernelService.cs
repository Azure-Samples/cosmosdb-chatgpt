using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Cosmos.Chat.GPT.Models;
using Microsoft.SemanticKernel.Embeddings;
using Azure.AI.OpenAI;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050, SKEXP0060, CS8600, CS8602, CS8619

namespace Cosmos.Chat.GPT.Services
{
    public class SemanticKernelService
    {
        //Semantic Kernel
        readonly Kernel kernel;

        private readonly string _systemPrompt = @"
        You are an AI assistant that helps people find information.
        Provide concise answers that are polite and professional.";

        private readonly string _summarizePrompt = @"
        Summarize this text. One to three words maximum length. 
        Plain text only. No punctuation, markup or tags.";


        public SemanticKernelService(string endpoint, string key, string completionDeploymentName, string embeddingDeploymentName)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
            ArgumentNullException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNullOrEmpty(completionDeploymentName);
            ArgumentNullException.ThrowIfNullOrEmpty(embeddingDeploymentName);

            // Initialize the Semantic Kernel
            kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(completionDeploymentName, endpoint, key)
                .AddAzureOpenAITextEmbeddingGeneration(embeddingDeploymentName, endpoint, key)
                .Build();

            //Add the Summarization plugin
            //kernel.Plugins.AddFromType<ConversationSummaryPlugin>();

            //summarizePlugin = new(kernel);

        }

        public async Task<(string completion, int tokens)> GetChatCompletionAsync(string sessionId, List<Message> chatHistory)
        {
            
            var skChatHistory = new ChatHistory();
            skChatHistory.AddSystemMessage(_systemPrompt);

            foreach (var message in chatHistory)
            {
                skChatHistory.AddUserMessage(message.Prompt);
                if(message.Completion != string.Empty)
                    skChatHistory.AddAssistantMessage(message.Completion);
            }

            PromptExecutionSettings settings = new()
            {
                ExtensionData = new Dictionary<string, object>()
                    {
                        { "Temperature", 0.2 },
                        { "TopP", 0.7 },
                        { "MaxTokens", 1000  }
                    }
            };

            
            var result = await kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(skChatHistory, settings);

            CompletionsUsage completionUsage = (CompletionsUsage)result.Metadata["Usage"]!;

            string completion = result.Items[0].ToString();
            int tokens = completionUsage.CompletionTokens;

            return (completion, tokens);

        }

        public async Task<float[]> GetEmbeddingsAsync(string text)
        {

            var embeddings = await kernel.GetRequiredService<ITextEmbeddingGenerationService>().GenerateEmbeddingAsync(text);

            //Convert ReadOnlyMemory<float> to float[]
            float[] embeddingsArray = embeddings.ToArray();

            return embeddingsArray;
        }

        public async Task<string> SummarizeConversationAsync(string conversation)
        {
            //return await summarizePlugin.SummarizeConversationAsync(conversation, kernel);

            var skChatHistory = new ChatHistory();
            skChatHistory.AddSystemMessage(_summarizePrompt);
            skChatHistory.AddUserMessage(conversation);

            PromptExecutionSettings settings = new()
            {
                ExtensionData = new Dictionary<string, object>()
                    {
                        { "Temperature", 0.0 },
                        { "TopP", 1.0 },
                        { "MaxTokens", 100 }
                    }
            };


            var result = await kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(skChatHistory, settings);

            string completion = result.Items[0].ToString();
            
            return completion;
        }
    }
}
