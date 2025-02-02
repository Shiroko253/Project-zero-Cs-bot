using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using dotenv.net;

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
            Console.WriteLine("❌ 錯誤：未找到 DISCORD_BOT_TOKEN，請檢查 .env 文件！");
            return;
        }

        _client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
        });

        _client.Log += LogAsync;
        _client.MessageReceived += HandleCommandAsync;

        await _client.LoginAsync(TokenType.Bot, botToken);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private async Task HandleCommandAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;

        string msg = message.Content.ToLower();

        if (msg == "!ping")
        {
            await message.Channel.SendMessageAsync("Pong!");
        }
        else if (msg.StartsWith("!echo "))
        {
            string text = message.Content.Substring(6);
            await message.Channel.SendMessageAsync(text);
        }
    }
}
