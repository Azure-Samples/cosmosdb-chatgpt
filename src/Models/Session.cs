using Newtonsoft.Json;

namespace Cosmos.Chat.GPT.Models;

public record Session
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string SessionId { get; set; }
    public int? Tokens { get; set; }
    public string Name { get; set; }
    [JsonIgnore]
    public List<Message> Messages { get; set; }
    public Session()
    {
        Id = Guid.NewGuid().ToString();
        Type = nameof(Session);
        SessionId = this.Id;
        Tokens = 0;
        Name = "New Chat";
        Messages = new List<Message>();
    }
}