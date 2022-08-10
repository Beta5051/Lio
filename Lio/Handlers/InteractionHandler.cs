using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Lio.Handlers;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _service;
    private readonly IServiceProvider _serviceProvider;

    public InteractionHandler(DiscordSocketClient client, InteractionService service, IServiceProvider serviceProvider)
    {
        _client = client;
        _service = service;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        _client.Ready += ReadyAsync;
        _service.Log += Utils.Log;

        await _service.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

        _client.InteractionCreated += HandleInteraction;
    }

    private async Task ReadyAsync() => await _service.RegisterCommandsGloballyAsync();

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);

            await _service.ExecuteCommandAsync(context, _serviceProvider);
        }
        catch
        {
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync()
                    .ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }
}