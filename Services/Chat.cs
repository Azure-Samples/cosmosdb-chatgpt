using Newtonsoft.Json;

namespace cosmosdb_chatgpt.Services
{

    public class ChatSession
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
            this.Id= Guid.NewGuid().ToString();
            this.Type = "ChatSession";
            this.ChatSessionId = this.Id;
            this.ChatSessionName = "New Chat";
			this.Messages = new List<ChatMessage>();
        }

        public void AddMessage(ChatMessage message) {
        
            Messages.Add(message);
        }
    }

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
}
