using BaseLib.Cards.Variables;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(Hook), "AfterCardPlayed")]
public static class RefundPatch
{
    public static async void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var refundAmount = cardPlay.Card.DynamicVars.TryGetValue(RefundVar.Key, out var val) ? val.IntValue : 0;
        if (refundAmount > 0 && cardPlay.Resources.EnergySpent > 0)
        {
            await PlayerCmd.GainEnergy(Math.Min(refundAmount, cardPlay.Resources.EnergySpent), cardPlay.Card.Owner);
        }
    }
}