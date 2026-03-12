using BaseLib.Cards.Variables;
using BaseLib.Utils.Patching;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(CardModel), "GetResultPileType")]
public static class ExhaustivePatch
{
    static void Postfix(CardModel __instance, ref PileType __result)
    {
        if (GetExhaustive(__instance) == 1)
        {
            __result = PileType.Exhaust;
        }
    }

    public static int GetExhaustive(CardModel card)
    {
        var exhaustiveAmount = card.DynamicVars.TryGetValue(ExhaustiveVar.Key, out var val) ? val.IntValue : 0;
        return ExhaustiveVar.ExhaustiveCount(card, exhaustiveAmount);
    }
}