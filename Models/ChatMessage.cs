using Newtonsoft.Json;

namespace Cosmos.Chat.GPT.Models;

public record ChatMessage
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    public string Type { get; set; }

    public string ChatSessionId { get; set; } //partition key

    public DateTime TimeStamp { get; set; }

    public string Sender { get; set; }

    public string Text { get; set; }

    public ChatMessage(string chatSessionId, string sender, string text)
    {
        Id = Guid.NewGuid().ToString();
        Type = "ChatMessage";
        ChatSessionId = chatSessionId; //partition key
        Sender = sender;
        TimeStamp = DateTime.UtcNow;
        Text = text;
    }
}