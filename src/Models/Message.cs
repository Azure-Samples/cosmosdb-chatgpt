namespace Cosmos.Chat.GPT.Models;

public record Message
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; }

    public string Type { get; set; }

    /// <summary>
    /// Partition key
    /// </summary>
    public string SessionId { get; set; }

    public DateTime TimeStamp { get; set; }

    public string Prompt { get; set; }

    public int PromptTokens { get; set; }

    public string Completion { get; set; }

    public int CompletionTokens { get; set; }
    public Message(string sessionId, int promptTokens, string prompt, string completion = "", int completionTokens = 0)
    {
        Id = Guid.NewGuid().ToString();
        Type = nameof(Message);
        SessionId = sessionId;
        TimeStamp = DateTime.UtcNow;
        Prompt = prompt;
        PromptTokens = promptTokens;
        Completion = completion;
        CompletionTokens = completionTokens;
    }
}