using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class PlayerExtensions
{
    public static bool HasPower<T>(this Player player) where T : PowerModel
    {
        return player.Creature.HasPower<T>();
    }
}