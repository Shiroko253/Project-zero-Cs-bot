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
        string botToken = Environment.GetEnvironmentVariable("MIAN_BOT_TOKEN");

        if (string.IsNullOrEmpty(botToken))
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
    {
        foreach (var guild in _client.Guilds)
        {
            var pingCommand = new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("測試機器人是否在線");

            var echoCommand = new SlashCommandBuilder()
                .WithName("echo")
                .WithDescription("回覆你輸入的文字")
                .AddOption("text", ApplicationCommandOptionType.String, "要回覆的文字", isRequired: true);

            try
            {
                await guild.CreateApplicationCommandAsync(pingCommand.Build());
                await guild.CreateApplicationCommandAsync(echoCommand.Build());
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
        switch (command.Data.Name)
        {
            case "ping":
                int latency = _client.Latency;
                await command.RespondAsync($"🏓 Pong! 當前機器人與discord api的延遲: {latency}ms", ephemeral: false);
                break;

            case "echo":
                string text = command.Data.Options.First().Value.ToString();
                await command.RespondAsync(text, ephemeral: false);
                break;
        }
    }
}
