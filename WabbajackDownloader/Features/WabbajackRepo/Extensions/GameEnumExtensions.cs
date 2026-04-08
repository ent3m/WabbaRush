using System.ComponentModel;
using System.Reflection;

namespace WabbajackDownloader.Features.WabbajackRepo;

internal static class GameEnumExtensions
{
    public static string GetDescription(this Game game)
    {
        FieldInfo? field = typeof(Game).GetField(game.ToString());
        DescriptionAttribute? description = field?.GetCustomAttribute<DescriptionAttribute>();
        return description?.Description ?? game.ToString();
    }
}
