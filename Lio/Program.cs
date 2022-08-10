using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lio.Handlers;
using Lio.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Victoria;
using Victoria.EventArgs;

namespace Lio;

public class Program
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    private readonly DiscordSocketConfig _socketConfig = new()
    {
        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.Guilds,
        AlwaysDownloadUsers = true,
    };

    private Program()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        _serviceProvider = new ServiceCollection()
            .AddSingleton(_configuration)
            .AddSingleton(_socketConfig)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<InteractionHandler>()
            .AddLavaNode()
            .AddSingleton<MusicService>()
            .BuildServiceProvider();
    }

    public static void Main(string[] args) => new Program().RunAsync().GetAwaiter().GetResult();

    private async Task RunAsync()
    {
        var client = _serviceProvider.GetRequiredService<DiscordSocketClient>();
        
        client.Log += Utils.Log;

        await _serviceProvider.GetRequiredService<InteractionHandler>().InitializeAsync();
        _serviceProvider.GetRequiredService<MusicService>().Initialize();

        await client.LoginAsync(TokenType.Bot, _configuration["Token"]);
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }
}