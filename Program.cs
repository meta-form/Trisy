using System.Reflection.Metadata.Ecma335;
using NetCord;
using NetCord.Gateway;
using NetCord.Logging;

string discordToken =
    Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new InvalidOperationException("DISCORD_TOKEN environment variable is not set.");

GatewayClient client = new(
    new BotToken(discordToken),
    new GatewayClientConfiguration
    {
        Intents =
            GatewayIntents.GuildMessages
            | GatewayIntents.MessageContent
            | GatewayIntents.GuildPresences,
    }
);

client.MessageCreate += async message =>
{
    Console.WriteLine(message.Author.Username + ": " + message.Content);

    if (message.Content.StartsWith("/insult"))
    {
        string msg = "Tu es un idiot.";

        await client.Rest.SendMessageAsync(message.ChannelId, msg);
        Console.WriteLine($"Bot sent {msg} in channel: {message.Channel}");
    }
    return;
};

client.PresenceUpdate += async presence =>
{
    Console.WriteLine($"User {presence.User.Username} is now {presence.Status}");

    if (presence.Status == UserStatusType.Online)
    {
        string msg = $"@{presence.User.Username} Je te vois.";

        NetCord.Rest.RestGuild guild = await client.Rest.GetGuildAsync(presence.GuildId);
        Console.WriteLine($"Fetched guild: {guild.Name}");

        ulong systemChannelId = guild.SystemChannelId ?? 0;
        Console.WriteLine($"System channel id: {systemChannelId}");
        Channel systemChannel = await client.Rest.GetChannelAsync(systemChannelId);
        Console.WriteLine($"Fetched channel: {systemChannel}");
        await client.Rest.SendMessageAsync(systemChannel.Id, msg);
        Console.WriteLine($"Bot sent {msg} in channel: {systemChannel}");
    }
    return;
};



await client.StartAsync();
await Task.Delay(-1);
