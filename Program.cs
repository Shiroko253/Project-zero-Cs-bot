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
        // ↓ Fix the problem ~~← "Peovleom"~~ here 
        string? botToken = Environment.GetEnvironmentVariable("MIAN_BOT_TOKEN"); // ← Converting null literal or possible null value to non-nullable type.
           // ↑ just missing the "?"
        if (string.IsNullOrEmpty(botToken))  // ← This part is great, no issues
        {
            Console.WriteLine("❌ 錯誤：未找到 MIAN_BOT_TOKEN，請檢查 .env 文件！");
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
            await RegisterCommands();
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

    private async Task RegisterCommands()
    {       // ↓ dereference of a possibly null reference.
        if (_client == null)
        {
            Console.WriteLine("❌ 錯誤：DiscordSocketClient 未初始化！");
            return;
        } // ↑ There was a bug, but I fixed it ↑
        
        foreach (var guild in _client.Guilds)
        {
            var pingCommand = new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("測試機器人是否在線");

            var echoCommand = new SlashCommandBuilder()
                .WithName("echo")
                .WithDescription("回覆你輸入的文字")
                .AddOption("text", ApplicationCommandOptionType.String, "要回覆的文字", isRequired: true);

            var shutdownCommand = new SlashCommandBuilder()
                .WithName("shutdown")
                .WithDescription("關閉機器人");

            var restartCommand = new SlashCommandBuilder()
                .WithName("restart")
                .WithDescription("重新啟動機器人");

            try
            {
                await guild.CreateApplicationCommandAsync(pingCommand.Build());
                await guild.CreateApplicationCommandAsync(echoCommand.Build());
                await guild.CreateApplicationCommandAsync(shutdownCommand.Build());
                await guild.CreateApplicationCommandAsync(restartCommand.Build());

                Console.WriteLine($"✅ 斜線指令已在伺服器 {guild.Name} 註冊！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 註冊指令到 {guild.Name} 時出錯：{ex.Message}");
            }
        }
    }

    private async Task HandleSlashCommandAsync(SocketSlashCommand command)
    {
        string? authorIdStr = Environment.GetEnvironmentVariable("AUTHOR_ID");
        if (string.IsNullOrEmpty(authorIdStr))
        {
            await command.RespondAsync("❌ 錯誤：未找到 AUTHOR_ID，請檢查 .env 文件！", ephemeral: true);
            return;
        }
        ulong authorId = ulong.Parse(authorIdStr);

        switch (command.Data.Name)
        {
            case "ping":
                int latency = _client!.Latency;
                await command.RespondAsync($"🏓 Pong! 當前機器人與discord api的延遲: {latency}ms", ephemeral: false);
                break;
            // ↓ here is big problems but is ok...
            case "echo":
                if (command.Data.Options.Count == 0 || command.Data.Options.First().Value == null) // ← Converting null literal or possible null value to non-nullable.
                {
                    await command.RespondAsync("❌ 錯誤：請提供有效的輸入！", ephemeral: true); // ← Looks like a new addition, but it's not explained clearly.
                    return;
                }

                string text = command.Data.Options.First().Value?.ToString() ?? "（無內容）";  // ← Here too
                await command.RespondAsync(text, ephemeral: false); // ← Here too but that is use await so what i can say
                break; // ← Don't forget this 'break;'
                          //  ↑ Do't forge this 'break;'

            case "shutdown":
                if (command.User.Id == authorId)
                {
                    await command.RespondAsync("\U0001f6d1 機器人即將關閉...", ephemeral: true);
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
                    await command.RespondAsync("🔄 機器人即將重新啟動...", ephemeral: true);
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
