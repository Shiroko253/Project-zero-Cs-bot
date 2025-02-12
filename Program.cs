using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using dotenv.net;
using Discord.Interactions;

class Program
{
    private DiscordSocketClient? _client;

    static async Task Main(string[] args) => await new Program().RunBotAsync();

    public async Task RunBotAsync()
    {
        DotEnv.Load();
        string? botToken = Environment.GetEnvironmentVariable("MIAN_BOT_TOKEN");

        if (string.IsNullOrEmpty(botToken))
        {
            Console.WriteLine("❌ Error: MIAN_BOT_TOKEN not found. Please check your .env file!");
            return;
        }

        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.Guilds
        };

        _client = new DiscordSocketClient(config);
        _client.Log += LogAsync;

        var commandService = new InteractionService(_client);
        _client.InteractionCreated += async interaction =>
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            await commandService.ExecuteCommandAsync(ctx, null);
        };

        _client.Ready += async () =>
        {
            Console.WriteLine("✅ Bot is ready!");

            await _client.SetStatusAsync(UserStatus.Idle);

            await _client.SetGameAsync("幽幽子大人", null, ActivityType.Watching);

            await ClearPreviousCommands();

            foreach (var guild in _client.Guilds)
            {
                await RegisterCommandsForGuild(guild.Id);
            }
        };

        _client.GuildAvailable += async guild =>
        {
            Console.WriteLine($"📢 Bot is now available in: {guild.Name}");
            await RegisterCommandsForGuild(guild.Id);
        };

        _client.SlashCommandExecuted += HandleSlashCommandAsync;

        await _client.LoginAsync(TokenType.Bot, botToken);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private async Task ClearPreviousCommands()
    {
        if (_client == null)
        {
            Console.WriteLine("❌ Error: DiscordSocketClient is not initialized!");
            return;
        }

        try
        {
            await _client.BulkOverwriteGlobalApplicationCommandsAsync(Array.Empty<ApplicationCommandProperties>());
            Console.WriteLine("✅ Cleared all global slash commands!");

            foreach (var guild in _client.Guilds)
            {
                await guild.BulkOverwriteApplicationCommandAsync(Array.Empty<ApplicationCommandProperties>());
                Console.WriteLine($"✅ Cleared all guild commands for {guild.Name}!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error while clearing commands: {ex.Message}");
        }
    }

    private async Task RegisterCommandsForGuild(ulong guildId)
    {
        var guild = _client!.GetGuild(guildId);
        if (guild == null) return;

        var pingCommand = new SlashCommandBuilder()
            .WithName("ping")
            .WithDescription("測試機器人是否在線");

        var echoCommand = new SlashCommandBuilder()
            .WithName("echo")
            .WithDescription("回覆你提供的文字")
            .AddOption("text", ApplicationCommandOptionType.String, "要回覆的文字", isRequired: true);

        var shutdownCommand = new SlashCommandBuilder()
            .WithName("shutdown")
            .WithDescription("關閉機器人");

        var restartCommand = new SlashCommandBuilder()
            .WithName("restart")
            .WithDescription("重新啟動機器人");

        try
        {
            var commands = new ApplicationCommandProperties[]
            {
                pingCommand.Build(),
                echoCommand.Build(),
                shutdownCommand.Build(),
                restartCommand.Build()
            };

            await guild.BulkOverwriteApplicationCommandAsync(commands);

            Console.WriteLine($"✅ Synchronized slash commands to guild {guild.Name}!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error registering commands to {guild.Name}: {ex.Message}");
        }
    }

    private async Task HandleSlashCommandAsync(SocketSlashCommand command)
    {
        string? authorIdStr = Environment.GetEnvironmentVariable("AUTHOR_ID");
        if (string.IsNullOrEmpty(authorIdStr))
        {
            await command.RespondAsync("❌ Error: AUTHOR_ID not found. Please check your .env file!", ephemeral: true);
            return;
        }
        ulong authorId = ulong.Parse(authorIdStr);

        switch (command.Data.Name)
        {
            case "ping":
                int latency = _client!.Latency;
                await command.RespondAsync($"🏓 Pong！目前與 Discord API 的延遲為：{latency}ms", ephemeral: false);
                break;

            case "echo":
                if (command.Data.Options.Count == 0 || command.Data.Options.First().Value == null)
                {
                    await command.RespondAsync("❌ 錯誤: 請提供有效的輸入！", ephemeral: true);
                    return;
                }

                string text = command.Data.Options.First().Value?.ToString() ?? "(無內容)";
                await command.RespondAsync(text, ephemeral: false);
                break;

            case "shutdown":
                if (command.User.Id == authorId)
                {
                    await command.RespondAsync("⛔ 機器人即將關閉...", ephemeral: true);
                    Environment.Exit(0);
                }
                else
                {
                    await command.RespondAsync("❌ 你沒有權限關閉機器人！", ephemeral: true);
                }
                break;

            case "restart":
                if (command.User.Id == authorId)
                {
                    await command.RespondAsync("🔄 機器人正在重新啟動...", ephemeral: true);
                    System.Diagnostics.Process.Start("dotnet", "run");
                    Environment.Exit(0);
                }
                else
                {
                    await command.RespondAsync("❌ 你沒有權限重新啟動機器人！", ephemeral: true);
                }
                break;
        }
    }
}
