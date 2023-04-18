using Newtonsoft.Json;

namespace Cosmos.Chat.GPT.Models;

public record Session
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

    public string Name { get; set; }

    [JsonIgnore]
    public List<Message> Messages { get; set; }

    public Session()
    {
        Id = Guid.NewGuid().ToString();
        Type = nameof(Session);
        SessionId = this.Id;
        Name = "New Chat";
        Messages = new List<Message>();
    }

    public void AddMessage(Message message)
    {
        Messages.Add(message);
    }
}