using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using NetCord;
using NetCord.Gateway;
using NetCord.Logging;
using NetCord.Rest;
using SQLitePCL;

// Start DB
SQLitePCL.Batteries.Init();
string leaderboardSqlFile = "Data Source=dbfiles\\leaderboard.db";
using var SqliteDb = new SqliteConnection(leaderboardSqlFile);
SqliteDb.Open();

DbCreateUserTable();
DbCreateUserInfoTable();

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
    if (message.Content.StartsWith("/gamerank"))
    {
        string msg = DbGetTop5GameTime();
        await message.ReplyAsync(msg);
        Console.WriteLine($"Bot sent {msg} to {message.Author.Username}");

        return;
    }
    if (message.Content.StartsWith("/help"))
    {
        string msg = DisplayHelp();
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

string DisplayHelp()
{
    return "./gamerank Permet de voir les 5 joueurs ayant passer le plus de temps à gamer\n"
    + "./insult Permet de te faire insulter\n"
    + "./compliment Permet de te faire complimenter\n";
}

void DbCreateUserTable()
{
    using var command = SqliteDb.CreateCommand();

    command.CommandText = @"CREATE TABLE IF NOT EXISTS User (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Username    TEXT    NOT NULL UNIQUE);";

    command.ExecuteNonQuery();
}

void DbCreateUserInfoTable()
{
    using var command = SqliteDb.CreateCommand();

    command.CommandText = @"CREATE TABLE IF NOT EXISTS UserInfo (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    Username  TEXT NOT NULL,
    GamePlayed TEXT NOT NULL,
    TimePlayedMins INTEGER NOT NULL);";

    command.ExecuteNonQuery();
}

void DbCreateUser(GuildUser member)
{
        using var command = SqliteDb.CreateCommand();
        command.CommandText = "SELECT Username FROM User where Username ='" + member.Username + "'";
        using (var query = command.ExecuteReader())
        {
            if(!query.Read())
            {
                using var insertCommand = SqliteDb.CreateCommand();
                // Le user n'existe pas donc on crée le user
                insertCommand.CommandText = "INSERT INTO User (Username) VALUES (@Username)";

                insertCommand.Parameters.Clear();
                insertCommand.Parameters.AddWithValue("@Username", member.Username);

                insertCommand.ExecuteNonQuery();
            }
        }
}

void DbCreateUserInfo(GuildUser member, UserActivity actv)
{
        using var command = SqliteDb.CreateCommand();
        command.CommandText = "SELECT Username FROM UserInfo where Username ='" + member.Username + "' AND GamePlayed = '" + actv.Name + "'";
        using (var query = command.ExecuteReader())
        {
            if(!query.Read())
            {
                using var insertCommand = SqliteDb.CreateCommand();
                // Le user n'existe pas donc on crée le user
                insertCommand.CommandText = "INSERT INTO UserInfo (Username, GamePlayed, TimePlayedMins) VALUES (@Username,@GamePlayed,@TimePlayedMins)";

                insertCommand.Parameters.Clear();
                insertCommand.Parameters.AddWithValue("@Username", member.Username);
                insertCommand.Parameters.AddWithValue("@GamePlayed", actv.Name);
                insertCommand.Parameters.AddWithValue("@TimePlayedMins", 0);

                insertCommand.ExecuteNonQuery();
            }
        }
}

/*
    using var command = SqliteDb.CreateCommand();
    command.CommandText = "SELECT * FROM UserInfo WHERE Username = '" + username + "'";
    */

string DbGetTop5GameTime()
{
    string msg;
    var usernames = new List<string>();
    var games = new List<string>();
    var timePlayedMins = new List<string>();

    using var command = SqliteDb.CreateCommand();
    command.CommandText = "SELECT Username,GamePlayed,TimePlayedMins FROM UserInfo ORDER BY TimePlayedMins DESC LIMIT 5";

    using var reader = command.ExecuteReader();
    while(reader.Read())
    {
        usernames.Add(reader["Username"].ToString());
        games.Add(reader["GamePlayed"].ToString());
        timePlayedMins.Add(reader["TimePlayedMins"].ToString());
    }

    msg = "Voici le top 5 des joueurs qui ont le plus jouer et leur jeu!\n";

    for (int i = 0; i < usernames.Count(); ++i)
    {
        msg += i + ". " + usernames[i] + " => " + games[i] + " :watch: " + timePlayedMins[i] + " mins\n";
    }

    return msg;
}

void DbUpdateTimeUserInfo(int minutesSpent, string gameName, GuildUser member)
{
    using var insertCommand = SqliteDb.CreateCommand();

    insertCommand.CommandText = "UPDATE UserInfo SET TimePlayedMins = TimePlayedMins + @TimePlayedMins WHERE Username = @Username AND GamePlayed = @GamePlayed;";

    insertCommand.Parameters.Clear();
    insertCommand.Parameters.AddWithValue("@Username", member.Username);
    insertCommand.Parameters.AddWithValue("@GamePlayed", gameName);
    insertCommand.Parameters.AddWithValue("@TimePlayedMins", minutesSpent);

    insertCommand.ExecuteNonQuery();
}

Dictionary<string,DateTimeOffset> timePlayed = new Dictionary<string,DateTimeOffset>();
Dictionary<string,string> gamePlayed = new Dictionary<string,string>();

client.PresenceUpdate += async userUpdate =>
{
    var guild = await client.Rest.GetGuildAsync(userUpdate.GuildId);
    var member = await client.Rest.GetGuildUserAsync(userUpdate.GuildId, userUpdate.User.Id);
    var playingActivity = userUpdate.Activities.FirstOrDefault(a => a.Type == UserActivityType.Playing);

    if (playingActivity != null)
    {
        /*string msg = $"@{member.Username} y joue a {playingActivity.Name} le sale";
        NetCord.Rest.RestGuild guild1 = await client.Rest.GetGuildAsync(userUpdate.GuildId);
        ulong systemChannelId = guild1.SystemChannelId ?? 0;
        Channel systemChannel = await client.Rest.GetChannelAsync(systemChannelId);
        await client.Rest.SendMessageAsync(systemChannel.Id, msg);*/

        Console.WriteLine($"{member.Username} plays {playingActivity.Name}");       
        DbCreateUser(member);
        DbCreateUserInfo(member, playingActivity);

        timePlayed.Add(member.Username, DateTimeOffset.UtcNow);
        gamePlayed.Add(member.Username, playingActivity.Name);
        Console.WriteLine("On détecte qu'on joue...");
    }
    else
    {
        Console.WriteLine($"{member.Username} stopped playing and is now {userUpdate.Status}");       

        DateTimeOffset startTime;
        TimeSpan timeSpent;
        string gameName= "";
        if(timePlayed.TryGetValue(member.Username, out startTime) && gamePlayed.TryGetValue(member.Username, out gameName));
        {
            timeSpent = DateTimeOffset.UtcNow - startTime;
            int minutesSpent = (int)timeSpent.TotalMinutes;
            Console.WriteLine(minutesSpent);
            DbUpdateTimeUserInfo(minutesSpent,gameName,member);
            Console.WriteLine("time should have updated!!!!!");
        }
    }
};

await client.StartAsync();
await Task.Delay(-1);
