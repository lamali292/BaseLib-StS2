using BaseLib.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
///     Your power can either inherit CustomPowerModel directly, or a different power class and ICustomPowerModel.
///     This class exists mainly to avoid needing to inherit multiple classes for most powers.
/// </summary>
public abstract class CustomPowerModel : PowerModel, ICustomPower, ILocalizationProvider, IHealthBarForecastSource
{
    public virtual string? CustomPackedIconPath => null; //64x64
    public virtual string? CustomBigIconPath => null; //256x256
    public virtual string? CustomBigBetaIconPath => null; //256x256

    /// <summary>
    ///     Override when this power wants to contribute extra health bar forecast segments.
    ///     Default is empty (no forecast).
    /// </summary>
    public virtual IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        return [];
    }

    /// <summary>
    ///     Override this to define localization directly in your class.
    ///     You are recommended to return a PowerLoc<seealso cref="PowerLoc" />.
    /// </summary>
    public virtual List<(string, string)>? Localization => null;
}