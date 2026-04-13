using System.Text.RegularExpressions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Patches.Localization;

[HarmonyPatch(typeof(LocManager), nameof(LocManager.LoadTable))]
public static partial class SimpleLoc
{
    private static readonly HashSet<string> SimpleLocEnabled = [];

    /// <summary>
    /// Call this in your mod's initializer to enable simple localization processing for all strings in your mod.
    /// </summary>
    /// <param name="modId"></param>
    public static void EnableSimpleLoc(string modId)
    {
        SimpleLocEnabled.Add(modId);
    }

    [HarmonyPostfix]
    static void ProcessSimpleLoc(string path, Dictionary<string, string>? __result)
    {
        if (__result == null) return;

        var pathElements = path.SimplifyPath().Split('/');
        int locIndex;
        for (locIndex = 0; locIndex < pathElements.Length; ++locIndex)
        {
            if (pathElements[locIndex] == "localization") break;
        }

        if (locIndex >= pathElements.Length || locIndex == 0) return;
        var modFolder = pathElements[locIndex - 1];

        bool modUseSimpleLoc = SimpleLocEnabled.Contains(modFolder);

        foreach (var key in __result.Keys.ToList())
        {
            var processed = __result[key];
            if (processed.StartsWith('#'))
            {
                __result[key] = Simplify(processed[1..]);
            }
            else if (modUseSimpleLoc)
            {
                __result[key] = Simplify(processed);
            }
        }
    }

    [GeneratedRegex(@"\*(.+?)\*?(\s|\b)")] private static partial Regex HighlightRegex { get; }

    [GeneratedRegex(@"!(.*?)!")] private static partial Regex DiffVariableRegex { get; }
    [GeneratedRegex(@"\?(.*?)\?")] private static partial Regex InverseVariableRegex { get; }

    [GeneratedRegex(@"({)([^{]*?)((?::.*)?}.*?)\((.*?)\)")]
    private static partial Regex PluralizeRegex { get; }

    private static readonly Dictionary<string, string> SpecialVarDictionary = new()
    {
        { "D", "Damage" },
        { "CD", "CalculatedDamage" },
        { "B", "Block" },
        { "CB", "CalculatedBlock" },
        { "C", "Cards" },
        { "E", "Energy" },
        { "H", "Heal" }
    };

    public static string TrySimplify(string loc)
    {
        return !loc.StartsWith('#') ? loc : Simplify(loc[1..]);
    }
    private static string Simplify(string loc)
    {
        loc = HighlightRegex.Replace(loc, "[gold]$1[/gold]$2");
        loc = DiffVariableRegex.Replace(loc, match => ReplaceVarName(match, ":diff()"));
        loc = InverseVariableRegex.Replace(loc, match => ReplaceVarName(match, ":inverseDiff()"));
        loc = PluralizeRegex.Replace(loc, "$1$2$3{$2:plural:|$4}");
            
        return loc;
    }

    private static string ReplaceVarName(Match match, string processor)
    {
        if (match.Groups.Count <= 1) return match.Value;

        var varText = match.Groups[1].Value;
        return $"{{{SpecialVarDictionary.GetValueOrDefault(varText, varText)}{processor}}}";
    }
}