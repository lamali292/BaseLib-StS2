using BaseLib.Patches.Content;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Abstracts;

public abstract class CustomAncientModel : AncientEventModel, ICustomModel
{
    //Suggested overrides: ButtonColor, DialogueColor
    
    public CustomAncientModel(bool autoAdd = true)
    {
        if (autoAdd) CustomContentDictionary.AddAncient(this);
    }

    /// <summary>
    /// Suggested to check act.ActNumber == 2 or 3.
    ///
    /// If you are overriding ShouldForceSpawn, you should override this and return false.
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    public virtual bool IsValidForAct(ActModel act) => true;
    
    /// <summary>
    /// Suggested to leave this set to false unless you want to force a specific ancient to spawn during map generation. Messing with this can cause mod conflicts, please only use if it is 100% necessary for your mod to function.
    /// </summary>
    /// <param name="act"></param>
    /// <param name="rngChosenAncient">The ancient that will have been chosen by the games rng.</param>
    /// <returns></returns>
    public virtual bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false;
    
    protected abstract OptionPools MakeOptionPools { get; }

    private OptionPools? _optionPools;
    public OptionPools OptionPools
    {
        get
        {
            if (_optionPools == null) _optionPools = MakeOptionPools;
            return _optionPools;
        }
    }
    
    public override IEnumerable<EventOption> AllPossibleOptions => 
        OptionPools.AllOptions.SelectMany(option => option.AllVariants.Select(relic => RelicOption(relic)));
    
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = OptionPools.Roll(Rng);
        return options.Select(option => RelicOption(option.ModelForOption)).ToList();
    }
    
    public static WeightedList<AncientOption> MakePool(params RelicModel[] options)
    {
        WeightedList<AncientOption> pool = [..options.Select(model => (AncientOption) model)];
        return pool;
    }
    public static WeightedList<AncientOption> MakePool(params AncientOption[] options)
    {
        WeightedList<AncientOption> pool = [..options];
        return pool;
    }

    public static AncientOption AncientOption<T>(int weight = 1, Func<T, RelicModel>? relicPrep = null, Func<T, IEnumerable<RelicModel>>? makeAllVariants = null) where T : RelicModel
    {
        return new AncientOption<T>(weight)
        {
            ModelPrep = relicPrep,
            Variants = makeAllVariants
        };
    }
    
    /******************    Assets    ******************/

    /// <summary>
    /// Override to load custom event scene.
    /// </summary>
    /// <param name="runState"></param>
    /// <returns></returns>
    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {
        var customScene = CustomScenePath;
        return customScene != null ? [customScene] : base.GetAssetPaths(runState);
    }

    /// <summary>
    /// Path to a custom event scene which will be the background of the event.
    /// </summary>
    public virtual string? CustomScenePath => null;

    public virtual string? CustomMapIconPath => null;
    public virtual string? CustomMapIconOutlinePath => null;

    public virtual Texture2D? CustomRunHistoryIcon => null;
    public virtual Texture2D? CustomRunHistoryIconOutline => null;


    /****************** Localization ******************/
    private string FirstVisit => $"{Id.Entry}.talk.firstvisitEver.0-0.ancient";
    private string BaseLocKeyForId(string id) => $"{Id.Entry}.talk.{id}.";

    private static string SfxPath(string dialogueLoc) =>
        LocString.GetIfExists("ancients", dialogueLoc + ".sfx")?.GetRawText() ?? "";
    
    protected override AncientDialogueSet DefineDialogues()
    {
        AncientDialogue firstVisit = new(SfxPath(FirstVisit));

        Dictionary<string, IReadOnlyList<AncientDialogue>> characterDialogues = [];
        
        foreach (CharacterModel character in ModelDb.AllCharacters)
        {
            var baseKey = BaseLocKeyForId(character.Id.Entry);
            characterDialogues[baseKey] = GetDialoguesForKey(baseKey);
        }
        
        return new AncientDialogueSet
        {
            FirstVisitEverDialogue = firstVisit,
            CharacterDialogues = characterDialogues,
            AgnosticDialogues = GetDialoguesForKey("ANY")
        };
    }

    private IReadOnlyList<AncientDialogue> GetDialoguesForKey(string baseKey)
    {
        List<AncientDialogue> dialogues = [];
        
        int index = 0;
        while (DialogueExists(baseKey, index))
        {
            List<string> sfxPaths = [];

            var line = ExistingLine(baseKey, index, sfxPaths.Count);

            while (line != null)
            {
                sfxPaths.Add(SfxPath(line));
                line = ExistingLine(baseKey, index, sfxPaths.Count);
            }
            
            dialogues.Add(new AncientDialogue(sfxPaths.ToArray()));    
            ++index;
        }

        return dialogues;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="baseKey">first section of key ending with a '.'</param>
    /// <param name="index">index of conversation</param>
    /// <returns></returns>
    private static bool DialogueExists(string baseKey, int index)
    {
        return LocString.Exists("ancients", $"{baseKey}{index}-0.ancient") ||
               LocString.Exists("ancients", $"{baseKey}{index}-0r.ancient") ||
               LocString.Exists("ancients", $"{baseKey}{index}-0.char") ||
               LocString.Exists("ancients", $"{baseKey}{index}-0r.char");
    }

    private static string? ExistingLine(string baseKey, int dialogueIndex, int lineIndex)
    {
        string locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}r.ancient";
        if (LocString.Exists("ancients", locEntry)) return locEntry;
        
        locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}r.char";
        if (LocString.Exists("ancients", locEntry)) return locEntry;
        
        locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}.ancient";
        if (LocString.Exists("ancients", locEntry)) return locEntry;
        
        locEntry = $"{baseKey}{dialogueIndex}-{lineIndex}.char";
        if (LocString.Exists("ancients", locEntry)) return locEntry;

        return null;
    }
}

[HarmonyPatch(typeof(AncientEventModel), "MapIconPath", MethodType.Getter)]
class MapIconPath
{
    [HarmonyPrefix]
    static bool Custom(AncientEventModel __instance, ref string? __result)
    {
        if (__instance is not CustomAncientModel custom)
            return true;

        __result = custom.CustomMapIconPath;
        return __result == null;
    }
}
[HarmonyPatch(typeof(AncientEventModel), "MapIconOutlinePath", MethodType.Getter)]
class MapIconOutlinePath
{
    [HarmonyPrefix]
    static bool Custom(AncientEventModel __instance, ref string? __result)
    {
        if (__instance is not CustomAncientModel custom)
            return true;

        __result = custom.CustomMapIconOutlinePath;
        return __result == null;
    }
}

[HarmonyPatch(typeof(AncientEventModel), "RunHistoryIcon", MethodType.Getter)]
class RunHistoryIcon
{
    [HarmonyPrefix]
    static bool Custom(AncientEventModel __instance, ref Texture2D? __result)
    {
        if (__instance is not CustomAncientModel custom)
            return true;

        __result = custom.CustomRunHistoryIcon;
        return __result == null;
    }
}
[HarmonyPatch(typeof(AncientEventModel), "RunHistoryIconOutline", MethodType.Getter)]
class RunHistoryIconOutline
{
    [HarmonyPrefix]
    static bool Custom(AncientEventModel __instance, ref Texture2D? __result)
    {
        if (__instance is not CustomAncientModel custom)
            return true;

        __result = custom.CustomRunHistoryIconOutline;
        return __result == null;
    }
}