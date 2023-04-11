using Newtonsoft.Json;

namespace Cosmos.Chat.GPT.Models;

public record ChatSession
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    public string Type { get; set; }

    public string ChatSessionId { get; set; } //partition key

    public string ChatSessionName { get; set; }

    [JsonIgnore]
    public List<ChatMessage> Messages { get; set; }

    public ChatSession()
    {
        Id = Guid.NewGuid().ToString();
        Type = "ChatSession";
        ChatSessionId = this.Id;
        ChatSessionName = "New Chat";
        Messages = new List<ChatMessage>();
    }

    public void AddMessage(ChatMessage message)
    {
        Messages.Add(message);
    }
}