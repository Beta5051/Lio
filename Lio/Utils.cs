using Discord;

namespace Lio;

public static class Utils
{
    public static Task Log(LogMessage message)
    {
        Console.WriteLine(message.ToString());
        return Task.CompletedTask;
    }

    public static Embed ErrorEmbed(Exception exception) => new EmbedBuilder
    {
        Title = "오류 :exclamation:",
        Description = exception.Message,
        Color = Color.Red,
    }.Build();
}