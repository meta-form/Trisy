using System.Runtime.CompilerServices;
using NetCord;
using NetCord.Gateway;
using NetCord.Logging;

string discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN") ?? throw new InvalidOperationException("DISCORD_TOKEN environment variable is not set.");
GatewayClient client = new(new BotToken(discordToken), new GatewayClientConfiguration
{
        Intents = GatewayIntents.GuildMessages 
            | GatewayIntents.MessageContent 
            | GatewayIntents.GuildPresences
            | GatewayIntents.GuildMessageTyping
            | GatewayIntents.DirectMessageTyping, 
});

client.MessageCreate += async message =>
{
    Console.WriteLine(message.Author.Username + ": " + message.Content);

    if (message.Content.StartsWith("/insult"))
    {
        string msg = "Tu es un idiot.";
        Console.WriteLine($"Bot sending {msg} in channel: {message.Channel}");
        await client.Rest.SendMessageAsync(message.ChannelId, msg);
    }
    return;
};

client.PresenceUpdate += async presence =>
{
    Console.WriteLine($"User {presence.User.Username} is now {presence.Status}");
    return;
};

client.TypingStart += async typing =>
{
    var user = typing.User;
    if(user != null)
    {   
        Console.WriteLine($"User {user.Username} is now typing");
    }
};



await client.StartAsync();
await Task.Delay(-1);