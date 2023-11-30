﻿using Azure;
using Azure.AI.OpenAI;
using Cosmos.Chat.GPT.Models;

namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service to access Azure OpenAI.
/// </summary>
public class OpenAiService
{
    private readonly string _modelName = String.Empty;
    private readonly OpenAIClient _client;

    /// <summary>
    /// System prompt to send with user prompts to instruct the model for chat session
    /// </summary>
    private readonly string _systemPrompt = @"
        You are an AI assistant that helps people find information.
        Provide concise answers that are polite and professional." + Environment.NewLine;

    /// <summary>    
    /// System prompt to send with user prompts to instruct the model for summarization
    /// </summary>
    private readonly string _summarizePrompt = @"
        Summarize this prompt in one or two words to use as a label in a button on a web page.
        Do not use any punctuation." + Environment.NewLine;

    /// <summary>
    /// Creates a new instance of the service.
    /// </summary>
    /// <param name="key">API key.</param>
    /// <param name="modelName">Name of the deployed Azure OpenAI model.</param>
    /// <exception cref="ArgumentNullException">Thrown when endpoint, key, or modelName is either null or empty.</exception>
    /// <remarks>
    /// This constructor will validate credentials and create a HTTP client instance.
    /// </remarks>
    public OpenAiService(string key, string modelName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(modelName);
        ArgumentNullException.ThrowIfNullOrEmpty(key);

        _modelName = modelName;

        _client = new(new AzureKeyCredential(key));
    }

    /// <summary>
    /// Sends a prompt to the deployed OpenAI LLM model and returns the response.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="userPrompt">Prompt message to send to the deployment.</param>
    /// <returns>Response from the OpenAI model along with tokens for the prompt and response.</returns>
    public async Task<(string completionText, int completionTokens)> GetChatCompletionAsync(string sessionId, string userPrompt)
    {

        ChatMessage systemMessage = new(ChatRole.System, _systemPrompt);
        ChatMessage userMessage = new(ChatRole.User, userPrompt);

        ChatCompletionsOptions options = new()
        {

            Messages =
            {
                systemMessage,
                userMessage
            },
            User = sessionId,
            MaxTokens = 2000,
            Temperature = 0.3f,
            NucleusSamplingFactor = 0.5f,
            FrequencyPenalty = 0,
            PresencePenalty = 0
        };

        Response<ChatCompletions> completionsResponse = await _client.GetChatCompletionsAsync(_modelName, options);

        ChatCompletions completions = completionsResponse.Value;

        return (
            completionText: completions.Choices[0].Message.Content,
            completionTokens: completions.Usage.CompletionTokens
        );
    }

    /// <summary>
    /// Sends the existing conversation to the OpenAI model and returns a two word summary.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="conversationText">conversation history to send to OpenAI.</param>
    /// <returns>Summarization response from the OpenAI model deployment.</returns>
    public async Task<string> SummarizeAsync(string sessionId, string conversationText)
    {

        ChatMessage systemMessage = new(ChatRole.System, _summarizePrompt);
        ChatMessage userMessage = new(ChatRole.User, conversationText);

        ChatCompletionsOptions options = new()
        {
            Messages = {
                systemMessage,
                userMessage
            },
            User = sessionId,
            MaxTokens = 200,
            Temperature = 0.0f,
            NucleusSamplingFactor = 1.0f,
            FrequencyPenalty = 0,
            PresencePenalty = 0
        };

        Response<ChatCompletions> completionsResponse = await _client.GetChatCompletionsAsync(_modelName, options);

        ChatCompletions completions = completionsResponse.Value;

        string completionText = completions.Choices[0].Message.Content;

        return completionText;
    }
}
