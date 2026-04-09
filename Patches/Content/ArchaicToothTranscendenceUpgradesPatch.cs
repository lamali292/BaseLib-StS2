using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.TranscendenceUpgrades),  MethodType.Getter)]
class ArchaicToothTranscendenceUpgradesPatch
{
    private static Dictionary<ModelId, CardModel>? _customTranscendence;
    
    [HarmonyPostfix]
    static void AddTranscendenceUpgradeForCustomCharacters(ref Dictionary<ModelId, CardModel> __result)
    {
        if (_customTranscendence == null)
        {
            _customTranscendence = [];
            foreach (var cardModel in ModelDb.AllCards)
            {
                if (cardModel is ICustomTranscendenceTarget target)
                {
                    _customTranscendence[cardModel.Id] = target.GetTranscendenceTransformedCard();
                }
            }
        }

        foreach (var entry in _customTranscendence)
        {
            __result[entry.Key] = entry.Value;
        }
    }
}