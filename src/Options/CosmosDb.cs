namespace Cosmos.Chat.GPT.Options;

public record CosmosDb
{
    public required string Endpoint { get; init; }

    public required string Key { get; init; }

    public required string Database { get; init; }

    public required string Container { get; init; }
};