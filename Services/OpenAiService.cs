using Azure;
using Azure.AI.OpenAI;

namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service to access Azure OpenAI.
/// </summary>
public class OpenAiService
{
    private readonly string _deploymentName = String.Empty;
    private readonly int _maxTokens = default;
    private readonly OpenAIClient _client;
    private readonly string _systemPromptText = @"
        You are an AI assistant that helps people find information.
        Provide concise answers that are polite and professional.
        If you do not know an answer, reply with ""I do not know the answer to your question.""
    ";
    private readonly string _summarizePromptText = @"Please summarize the following text into two words.";

    /// <summary>
    /// Gets the maximum number of tokens.
    /// </summary>
    public int MaxTokens
    {
        get => _maxTokens;
    }

    /// <summary>
    /// Creates a new instance of the service.
    /// </summary>
    /// <param name="endpoint">Endpoint URI.</param>
    /// <param name="key">Account key.</param>
    /// <param name="deploymentName">Name of the deployment access.</param>
    /// <param name="maxTokens">Maximum number of tokens per request.</param>
    /// <exception cref="ArgumentNullException">Thrown when endpoint, key, deploymentName, or maxTokens is either null or empty.</exception>
    /// <remarks>
    /// This constructor will validate credentials and create a HTTP client instance.
    /// </remarks>
    public OpenAiService(string endpoint, string key, string deploymentName, string maxTokens)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNullOrEmpty(deploymentName);
        ArgumentNullException.ThrowIfNullOrEmpty(maxTokens);

        _deploymentName = deploymentName;
        _maxTokens = Int32.TryParse(maxTokens, out _maxTokens) ? _maxTokens : 3000;

        _client = new(new Uri(endpoint), new AzureKeyCredential(key));
    }

    /// <summary>
    /// Sends a prompt to the AI model deployment and returns the response.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="prompt">Prompt message to send to the deployment.</param>
    /// <returns>Response from the AI model deployment along with tokens for the prompt and response.</returns>
    public async Task<(string response, int promptTokens, int responseTokens)> AskAsync(string sessionId, string prompt)
    {
        ChatMessage systemPrompt = new(ChatRole.System, _systemPromptText);
        ChatMessage userPrompt = new(ChatRole.User, prompt);

        ChatCompletionsOptions options = new()
        {
            Messages = {
                systemPrompt,
                userPrompt
            },
            User = sessionId,
            MaxTokens = _maxTokens,
            Temperature = 0.5f,
            NucleusSamplingFactor = 0.95f,
            FrequencyPenalty = 0,
            PresencePenalty = 0
        };

        Response<ChatCompletions> completionsResponse = await _client.GetChatCompletionsAsync(_deploymentName, options);

        ChatCompletions completions = completionsResponse.Value;

        return (
            response: completions.Choices[0].Message.Content,
            promptTokens: completions.Usage.PromptTokens,
            responseTokens: completions.Usage.CompletionTokens
        );
    }

    /// <summary>
    /// Sends the existing conversation to the AI model deployment and returns a brief summary.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="conversation">Prompt conversation to send to the deployment.</param>
    /// <returns>Summarization response from the AI model deployment.</returns>
    public async Task<string> SummarizeAsync(string sessionId, string prompt)
    {
        ChatMessage systemPrompt = new(ChatRole.System, _systemPromptText);
        ChatMessage summarizePrompt = new(ChatRole.User, _summarizePromptText);
        ChatMessage userPrompt = new(ChatRole.User, prompt);

        ChatCompletionsOptions options = new()
        {
            Messages = {
                //systemPrompt,
                summarizePrompt,
                userPrompt
            },
            User = sessionId,
            MaxTokens = _maxTokens,
            Temperature = 0.5f,
            NucleusSamplingFactor = 0.95f,
            FrequencyPenalty = 0,
            PresencePenalty = 0
        };

        Response<ChatCompletions> completionsResponse = await _client.GetChatCompletionsAsync(_deploymentName, options);

        ChatCompletions completions = completionsResponse.Value;

        string summary =  completions.Choices[0].Message.Content.TrimEnd('.');

        return summary;
    }
}