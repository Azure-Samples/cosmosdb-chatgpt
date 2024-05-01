namespace Cosmos.Chat.GPT.Models
{
    public record CacheItem
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; }

        public float[] Vectors { get; set; }
        public string Prompts { get; set; }

        public string Completion { get; set; }

        public CacheItem(float[] vectors, string prompts, string completion)
        {
            Id = Guid.NewGuid().ToString();
            Vectors = vectors;
            Prompts = prompts;
            Completion = completion;
        }
    }
}
