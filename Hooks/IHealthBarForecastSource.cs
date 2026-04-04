using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BaseLib.Hooks;

public enum HealthBarForecastDirection
{
    FromRight = 0,
    FromLeft = 1
}

public readonly record struct HealthBarForecastSegment(
    int Amount,
    Color Color,
    HealthBarForecastDirection Direction,
    int Order = 0
);

public readonly record struct HealthBarForecastContext(Creature Creature)
{
    public CombatState? CombatState => Creature.CombatState;
    public CombatSide? CurrentSide => Creature.CombatState?.CurrentSide;
}

public static class HealthBarForecastOrder
{
    public static int ForSideTurnStart(Creature creature, CombatSide triggerSide)
    {
        ArgumentNullException.ThrowIfNull(creature);
        return creature.CombatState?.CurrentSide == triggerSide ? 1 : 0;
    }

    public static int ForSideTurnEnd(Creature creature, CombatSide triggerSide)
    {
        ArgumentNullException.ThrowIfNull(creature);
        return creature.CombatState?.CurrentSide == triggerSide ? 0 : 1;
    }
}

public interface IHealthBarForecastSource
{
    IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context);
}