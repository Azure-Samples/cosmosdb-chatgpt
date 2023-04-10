using Newtonsoft.Json;

namespace Cosmos.Chat.Models;

public class ChatMessage
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    public string Type { get; set; }

    public string ChatSessionId { get; set; } //partition key

    public DateTime TimeStamp { get; set; }

    public string Sender { get; set; }

    public string Text { get; set; }

    public ChatMessage(string ChatSessionId, string Sender, string Text)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Type = "ChatMessage";
        this.ChatSessionId = ChatSessionId; //partition key
        this.Sender = Sender;
        this.TimeStamp = DateTime.UtcNow;
        this.Text = Text;
    }
}