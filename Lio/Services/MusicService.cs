using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace Lio.Services;

public class MusicService
{
    private readonly LavaNode _lavaNode;
    private readonly DiscordSocketClient _client;

    public MusicService(LavaNode lavaNode, DiscordSocketClient client)
    {
        _lavaNode = lavaNode;
        _client = client;
    }

    public void Initialize()
    {
        _client.Ready += ReadyAsync;
        _lavaNode.OnLog += Utils.Log;
        _lavaNode.OnTrackEnded += TrackEnded;
    }

    private async Task ReadyAsync() => await _lavaNode.ConnectAsync();
    
    private async Task TrackEnded(TrackEndedEventArgs arg)
    {
        if (arg.Reason != TrackEndReason.Finished) return;

        if (!arg.Player.Queue.TryDequeue(out var queue)) return;

        if (queue == null) return;

        await arg.Player.PlayAsync(queue);
        await arg.Player.TextChannel.SendMessageAsync($"{queue.Title} 을 재생합니다.");
    }

    public async Task PlayAsync(SocketInteractionContext context, SearchType type, string query)
    {
        var voiceState = context.User as IVoiceState;

        if (voiceState?.VoiceChannel == null)
        {
            await context.Interaction.RespondAsync("음성 채널에 입장후 사용해주세요.");
            return;
        }

        if (!_lavaNode.HasPlayer(context.Guild))
        {
            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, context.Channel as ITextChannel);
            }
            catch (Exception exception)
            {
                await context.Interaction.RespondAsync(embed: Utils.ErrorEmbed(exception));
                return;
            }
        }

        var player = _lavaNode.GetPlayer(context.Guild);

        if (player.VoiceChannel != voiceState.VoiceChannel)
        {
            if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
            {
                await context.Interaction.RespondAsync("이미 다른 음성 채널에서 봇을 사용중입니다.");
                return;
            }

            try
            {
                await _lavaNode.MoveChannelAsync(voiceState.VoiceChannel);
            }
            catch (Exception exception)
            {
                await context.Interaction.RespondAsync(embed: Utils.ErrorEmbed(exception));
                return;
            }
        }

        var search = await _lavaNode.SearchAsync(type, query);

        if (search.Status == SearchStatus.NoMatches)
        {
            await context.Interaction.RespondAsync("해당 음악이 존재하지 않습니다.");
            return;
        }

        var track = search.Tracks.First();

        if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
        {
            player.Queue.Enqueue(track);
            await context.Interaction.RespondAsync($"{track.Title} 이 대기열에 추가되었습니다.");
            return;
        }

        await player.PlayAsync(track);

        await context.Interaction.RespondAsync($"{player.Track.Title} 을 재생합니다.");
    }

    public async Task InfoAsync(SocketInteractionContext context)
    {
        if (!_lavaNode.HasPlayer(context.Guild))
        {
            await context.Interaction.RespondAsync("봇이 음성 채널에 연결되어 있지않습니다.");
            return;
        }

        var player = _lavaNode.GetPlayer(context.Guild);
        var track = player.Track;
        if (track == null)
        {
            await context.Interaction.RespondAsync("음악이 재생중이지 않습니다.");
            return;
        }

        await context.Interaction.RespondAsync(embed: new EmbedBuilder
        {
            Title = $"{track.Title} ({track.Author})",
            Url = track.Url,
            ImageUrl = await track.FetchArtworkAsync(),
            Footer = new EmbedFooterBuilder
            {
                Text = $"{track.Position:hh\\:mm\\:ss}/{track.Duration:hh\\:mm\\:ss}"
            },
        }.Build());
    }

    public async Task SkipAsync(SocketInteractionContext context)
    {
        if (!_lavaNode.HasPlayer(context.Guild))
        {
            await context.Interaction.RespondAsync("봇이 음성 채널에 연결되어 있지않습니다.");
            return;
        }

        var player = _lavaNode.GetPlayer(context.Guild);
        if (player.Queue.Count == 0)
        {
            await context.Interaction.RespondAsync("대기열에 음악이 존재하지않습니다.");
            return;
        }

        var (_, current) = await player.SkipAsync();
        await context.Interaction.RespondAsync($"{current.Title} 을 재생합니다.");
    }
}