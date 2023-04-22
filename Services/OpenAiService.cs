using Azure;
using Azure.AI.OpenAI;
using Cosmos.Chat.GPT.Models;

namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service to access Azure OpenAI.
/// </summary>
public class OpenAiService
{
    private readonly string _deploymentName = String.Empty;
    private readonly int _maxTokens = default;
    private readonly OpenAIClient _client;
    private readonly string _systemPrompt = @"
        You are an AI assistant that helps people find information.
        Provide concise answers that are polite and professional." + Environment.NewLine;
    private readonly string _summarizePrompt = @"
        Summarize this prompt in one or two words to use as a label in a button on a web page" + Environment.NewLine;

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
    public async Task<(string response, int promptTokens, int responseTokens)> AskAsync(string sessionId, string userPrompt)
    {

        string prompt = _systemPrompt + userPrompt;
        
        CompletionsOptions options = new()
        {
            
            Prompts =
            {
                prompt
            },
            User = sessionId,
            MaxTokens = 256,
            Temperature = 0.3f,
            NucleusSamplingFactor = 0.5f,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            ChoicesPerPrompt = 1
        };

        Response<Completions> completionsResponse = await _client.GetCompletionsAsync(_deploymentName, options);


        Completions completions = completionsResponse.Value;

        return (
            response: completions.Choices[0].Text,
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
    public async Task<string> SummarizeAsync(string sessionId, string userPrompt)
    {
        string prompt = _summarizePrompt + userPrompt;
        
        CompletionsOptions options = new()
        {
            Prompts = { 
                prompt 
            },
            User = sessionId,
            MaxTokens = 200,
            Temperature = 0.0f,
            NucleusSamplingFactor = 1.0f,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            ChoicesPerPrompt = 1
        };

        Response<Completions> completionsResponse = await _client.GetCompletionsAsync(_deploymentName, options);

        Completions completions = completionsResponse.Value;

        string summary =  completions.Choices[0].Text;

        return summary;
    }
}