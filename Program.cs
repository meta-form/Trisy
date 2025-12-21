using System.Diagnostics;
using System.Runtime.CompilerServices;
using NetCord;
using NetCord.Gateway;
using NetCord.Logging;
using NetCord.Rest;

string discordToken =
    Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new InvalidOperationException("DISCORD_TOKEN environment variable is not set.");

string dataJson = File.ReadAllText("data.json");
var data =
    System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(dataJson)
    ?? throw new InvalidOperationException("Failed to deserialize data.json.");

Console.WriteLine("---Bot started---");
GatewayClient client = new(
    new BotToken(discordToken),
    new GatewayClientConfiguration
    {
        Intents =
            GatewayIntents.GuildMessages
            | GatewayIntents.MessageContent
            | GatewayIntents.GuildPresences
            | GatewayIntents.GuildMessageTyping
            | GatewayIntents.DirectMessageTyping,
    }
);

client.MessageCreate += async message =>
{
    Console.WriteLine(message.Author.Username + ": " + message.Content);

    if (message.Content.StartsWith("/insult"))
    {
        if (data["insults"].Count == 0)
            return;

        string msg = data["insults"][new Random().Next(data["insults"].Count)];
        await message.ReplyAsync(msg);
        Console.WriteLine($"Bot sent {msg} to {message.Author.Username}");

        return;
    }
    if (message.Content.StartsWith("/compliment"))
    {
        if (data["compliments"].Count == 0)
            return;

        string msg = data["compliments"][new Random().Next(data["compliments"].Count)];
        await message.ReplyAsync(msg);
        Console.WriteLine($"Bot sent {msg} to {message.Author.Username}");

        return;
    }
};

client.PresenceUpdate += async presence =>
{
    Console.WriteLine($"User {presence.User.Username} is now {presence.Status}");

    // if (presence.User.Username.Trim() != "" && presence.Status == UserStatusType.Online)
    // {
    //     string msg = $"@{presence.User.Username} Je te vois.";

    //     NetCord.Rest.RestGuild guild = await client.Rest.GetGuildAsync(presence.GuildId);
    //     Console.WriteLine($"Fetched guild: {guild.Name}");

    //     ulong systemChannelId = guild.SystemChannelId ?? 0;
    //     Console.WriteLine($"System channel id: {systemChannelId}");
    //     Channel systemChannel = await client.Rest.GetChannelAsync(systemChannelId);
    //     Console.WriteLine($"Fetched channel: {systemChannel}");
    //     await client.Rest.SendMessageAsync(systemChannel.Id, msg);
    //     Console.WriteLine($"Bot sent {msg} in channel: {systemChannel}");
    // }
    return;
};

client.TypingStart += async typing =>
{
    var user = typing.User;
    if (false && user != null) // Deactivated
    {
        Console.WriteLine($"User {user.Username} is now typing");
        string msg = $"@{user.Username} J'te vois écrire sale animal";
        NetCord.Rest.RestGuild guild = await client.Rest.GetGuildAsync(user.GuildId);
        ulong systemChannelId = guild.SystemChannelId ?? 0;
        Channel systemChannel = await client.Rest.GetChannelAsync(systemChannelId);
        await client.Rest.SendMessageAsync(systemChannel.Id, msg);
    }
};

client.PresenceUpdate += async userUpdate =>
{
    foreach (var activity in userUpdate.Activities)
    {
        string msg = $"@{userUpdate.User.Username} y joue a {activity.Name} le sale";
        NetCord.Rest.RestGuild guild = await client.Rest.GetGuildAsync(userUpdate.GuildId);
        ulong systemChannelId = guild.SystemChannelId ?? 0;
        Channel systemChannel = await client.Rest.GetChannelAsync(systemChannelId);
        await client.Rest.SendMessageAsync(systemChannel.Id, msg);

        var time = activity.Timestamps.StartTime;
        var end = activity.Timestamps.EndTime;

        Console.WriteLine("debut " + time);
        Console.WriteLine("fin " + end);
    }
};

await client.StartAsync();
await Task.Delay(-1);
