using NetCord;
using NetCord.Gateway;
using NetCord.Logging;

string discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN") ?? throw new InvalidOperationException("DISCORD_TOKEN environment variable is not set.");
GatewayClient client = new(new BotToken(discordToken), new GatewayClientConfiguration
{
    Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent,
});

client.MessageCreate += message =>
{
    Console.WriteLine(message.Author.Username + ": " + message.Content);
    return default;
};

await client.StartAsync();
await Task.Delay(-1);