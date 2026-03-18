using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace BaseLib.Extensions;

public static class FloatExtensions
{
    public static float OrFast(this float time)
    {
        return SaveManager.Instance.PrefsSave.FastMode switch
        {
            FastModeType.Instant => 0.01f,
            FastModeType.Fast => time * 0.3f,
            _ => time
        };
    }
}