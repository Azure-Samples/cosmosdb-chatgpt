namespace Cosmos.Chat.Options;

public record OpenAi
{
    public required string Endpoint { get; init; }

    public required string Key { get; init; }

    public required string Deployment { get; init; }

    public required string MaxTokens { get; init; }
};