namespace Cosmos.Chat.GPT.Options;

public record OpenAi
{
    public required string Endpoint { get; init; }

    public required string Key { get; init; }

    public required string Deployment { get; init; }

    public string? MaxConversationTokens { get; init; }
}