using BaseLib.Extensions;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Localization;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
public class ExtraTooltips
{
    [HarmonyTranspiler]
    static List<CodeInstruction> AddCustomTips(IEnumerable<CodeInstruction> instructions)
    {
        return new InstructionPatcher(instructions)
            .Match(new InstructionMatcher()
                .ldarg_0()
                .callvirt(AccessTools.PropertyGetter(typeof(CardModel), "ExtraHoverTips"))
                .call(null)
                .stloc_0()
            )
            .Insert([
                CodeInstruction.LoadLocal(0), //Load stored list
                CodeInstruction.LoadArgument(0), //Load card
                CodeInstruction.Call(typeof(ExtraTooltips), "AddTips"), //add tips to list
            ]);
    }

    public static void AddTips(List<IHoverTip> tips, CardModel card)
    {
        //dynvar tips
        foreach (var dynVar in card.DynamicVars.Values)
        {
            var tip = DynamicVarExtensions.DynamicVarTips[dynVar]?.Invoke();
            if (tip != null) tips.Add(tip);
        }
    }
}