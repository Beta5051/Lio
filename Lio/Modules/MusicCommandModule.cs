using Discord;
using Discord.Interactions;
using Lio.Services;
using Microsoft.Extensions.Configuration;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Search;

namespace Lio.Modules;

public class MusicCommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MusicService _service;

    public MusicCommandModule(MusicService service) => _service = service;

    [SlashCommand("music_play", "음악을 재생합니다.")]
    public async Task PlayAsync(string query, SearchType type = SearchType.YouTube) =>
        await _service.PlayAsync(Context, type, query);

    [SlashCommand("music_info", "현재 음악 정보를 봅니다.")]
    public async Task InfoAsync() => await _service.InfoAsync(Context);

    [SlashCommand("music_skip", "현재 음악을 스킵합니다.")]
    public async Task SkipAsync() => await _service.SkipAsync(Context);
}