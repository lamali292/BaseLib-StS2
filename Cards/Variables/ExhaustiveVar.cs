using System.Diagnostics;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Cards.Variables;

public class ExhaustiveVar : DynamicVar
{
    
    public const string Key = "Exhaustive";

    public ExhaustiveVar(decimal exhaustiveCount) : base(Key, exhaustiveCount)
    {
        this.WithTooltip();
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        PreviewValue = ExhaustiveCount(card, IntValue);
    }
    
    public static int ExhaustiveCount(CardModel card, int baseExhaustive)
    {
        if (baseExhaustive <= 0)
            return 0;
        int playCount = CombatManager.Instance.History.CardPlaysFinished.Count((entry) => entry.CardPlay.Card == card);
        return Math.Max(1, baseExhaustive - playCount);
    }
}