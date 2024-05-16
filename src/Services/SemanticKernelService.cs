using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Cosmos.Chat.GPT.Models;
using Microsoft.SemanticKernel.Embeddings;
using Azure.AI.OpenAI;

#pragma warning disable SKEXP0010, SKEXP0001

namespace Cosmos.Chat.GPT.Services
{
    public class SemanticKernelService
    {
        readonly Kernel kernel;

        private readonly string _systemPrompt = @"
        You are an AI assistant that helps people find information.
        Provide concise answers that are polite and professional.";

        private readonly string _summarizePrompt = @"
        Summarize this text. One to three words maximum length. 
        Plain text only. No punctuation, markup or tags.";

        public SemanticKernelService(string endpoint, string key, string completionDeploymentName, string embeddingDeploymentName)
        {
            _systemPrompt += "";
            ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
            ArgumentNullException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNullOrEmpty(completionDeploymentName);
            ArgumentNullException.ThrowIfNullOrEmpty(embeddingDeploymentName);

            kernel = Kernel.CreateBuilder()
                .Build();
        }

        public async Task<float[]> GetEmbeddingsAsync(string text)
        {
            await Task.Delay(0);
            float[] embeddingsArray = new float[0];

            return embeddingsArray;
        }

        public async Task<(string completion, int tokens)> GetChatCompletionAsync(string sessionId, List<Message> chatHistory)
        {
            var skChatHistory = new ChatHistory();
            skChatHistory.AddSystemMessage(string.Empty);

            foreach (var message in chatHistory)
            {
                skChatHistory.AddUserMessage(message.Prompt);
                skChatHistory.AddAssistantMessage(string.Empty);
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

            string completion = "Place holder response";
            int tokens = 0;
            await Task.Delay(0);

            return (completion, tokens);
        }

        public async Task<string> SummarizeAsync(string conversation)
        {
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
            string completion = result.Items[0].ToString()!;
            return completion;
        }
    }
}
