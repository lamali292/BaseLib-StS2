using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.UI;

public record RelicIconData(
    string? BigIconPath,
    string? PackedIconPath,
    string? PackedIconOutlinePath);

[HarmonyPatch]
public class RelicImageOverridePatch
{
    private static Dictionary<Type, List<(RelicIconData, Func<RelicModel, bool>?)>> _relicImageOverrides = [];
    
    /// <summary>
    /// Adds overriding file paths for a relic's images.
    /// </summary>
    public static void AddOverride<TRelicType>(RelicIconData data, Func<RelicModel, bool>? condition = null) where TRelicType : RelicModel
    {
        if (!_relicImageOverrides.TryGetValue(typeof(TRelicType), out var list))
        {
            list = [];
            _relicImageOverrides[typeof(TRelicType)] = list;
        }
        
        list.Add((data, condition));
    }
    
    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.PackedIconPath), MethodType.Getter)]
    [HarmonyPrefix]
    static bool PackedIconPath(RelicModel __instance, ref string? __result)
    {
        return TryGetCustomPath(__instance, y => y.PackedIconPath, ref __result);
    }

    [HarmonyPatch(typeof(RelicModel), "PackedIconOutlinePath", MethodType.Getter)]
    [HarmonyPrefix]
    static bool PackedIconOutlinePath(RelicModel __instance, ref string? __result)
    {
        return TryGetCustomPath(__instance, y => y.PackedIconOutlinePath, ref __result);
    }
    
    [HarmonyPatch(typeof(RelicModel), "BigIconPath", MethodType.Getter)]
    [HarmonyPrefix]
    static bool BigIconPath(RelicModel __instance, ref string? __result)
    {
        return TryGetCustomPath(__instance, y => y.BigIconPath, ref __result);
    }

    static bool TryGetCustomPath(RelicModel relic, Func<RelicIconData, string?> selector, ref string? result)
    {
        if (!_relicImageOverrides.TryGetValue(relic.GetType(), out var overrides)) return true;

        foreach (var overrideData in overrides)
        {
            if (overrideData.Item2 == null || overrideData.Item2(relic))
            {
                result = selector(overrideData.Item1);
                return result == null;
            }
        }

        return true;
    }
}
