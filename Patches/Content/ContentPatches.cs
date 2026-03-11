using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.InitIds))]
public static class CustomContentDictionary
{
    private static readonly Dictionary<Type, int> CustomModelCounts = []; //May log, may just remove.
    private static readonly Dictionary<Type, Type> PoolTypes = [];
    
    public static readonly List<CustomAncientModel> CustomAncients = [];
    
    static CustomContentDictionary()
    {
        PoolTypes.Add(typeof(CardPoolModel), typeof(CardModel));
        PoolTypes.Add(typeof(RelicPoolModel), typeof(RelicModel));
        PoolTypes.Add(typeof(PotionPoolModel), typeof(PotionModel));
    }


    public static void AddModel(Type modelType)
    {
        var poolAttribute = modelType.GetCustomAttribute<PoolAttribute>()
            ?? throw new Exception($"Model {modelType.FullName} must be marked with a PoolAttribute to determine which pool to add it to.");

        if (!IsValidPool(modelType, poolAttribute.PoolType))
        {
            throw new Exception($"Model {modelType.FullName} is assigned to incorrect type of pool {poolAttribute.PoolType.FullName}.");
        }

        int count = CustomModelCounts.GetValueOrDefault(poolAttribute.PoolType, 0);
        CustomModelCounts[poolAttribute.PoolType] = count + 1;
        
        ModHelper.AddModelToPool(poolAttribute.PoolType, modelType);
    }

    public static void AddAncient(CustomAncientModel ancient)
    {
        int count = CustomModelCounts.GetValueOrDefault(typeof(CustomAncientModel), 0);
        CustomModelCounts[typeof(CustomAncientModel)] = count + 1;
        CustomAncients.Add(ancient);
    }
    
    
    private static bool IsValidPool(Type modelType, Type poolType)
    {
        var basePoolType = poolType.BaseType;
        while (basePoolType != null)
        {
            if (PoolTypes.TryGetValue(basePoolType, out var poolValueType))
            {
                return modelType.IsAssignableTo(poolValueType);
            }
            basePoolType = basePoolType.BaseType;
        }
        throw new Exception($"Model {modelType.FullName} is assigned to {poolType.FullName} which is not a valid pool type.");
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllSharedAncients), MethodType.Getter)]
class CustomAncientsInPoolPatch
{
    [HarmonyPostfix]
    static IEnumerable<AncientEventModel> AddCustomPools(IEnumerable<AncientEventModel> __result)
    {
        return [.. __result, .. CustomContentDictionary.CustomAncients];
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.SetSharedAncientSubset))]
class FilterCustomAncients
{
    [HarmonyPrefix]
    static void RemoveInvalid(ActModel __instance, List<AncientEventModel> sharedAncientSubset)
    {
        sharedAncientSubset.RemoveAll(ancient =>
            ancient is CustomAncientModel customAncient && !customAncient.IsValidForAct(__instance));
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllSharedCardPools), MethodType.Getter)]
class ModelDbSharedCardPoolsPatch
{
    private static readonly List<CardPoolModel> CustomSharedPools = [];

    [HarmonyPostfix]
    static IEnumerable<CardPoolModel> AddCustomPools(IEnumerable<CardPoolModel> __result)
    {
        return [.. __result, .. CustomSharedPools];
    }

    public static void Register(CustomCardPoolModel pool)
    {
        CustomSharedPools.Add(pool);
    }
}

[HarmonyPatch(typeof(ModelDb), "AllSharedRelicPools", MethodType.Getter)]
class ModelDbSharedRelicPoolsPatch
{
    private static readonly List<RelicPoolModel> customSharedPools = [];

    [HarmonyPostfix]
    static IEnumerable<RelicPoolModel> AddCustomPools(IEnumerable<RelicPoolModel> __result)
    {
        return [.. __result, .. customSharedPools];
    }

    public static void Register(CustomRelicPoolModel pool)
    {
        customSharedPools.Add(pool);
    }
}

[HarmonyPatch(typeof(ModelDb), "AllSharedPotionPools", MethodType.Getter)]
class ModelDbSharedPotionPoolsPatch
{
    private static readonly List<PotionPoolModel> customSharedPools = [];

    [HarmonyPostfix]
    static IEnumerable<PotionPoolModel> AddCustomPools(IEnumerable<PotionPoolModel> __result)
    {
        return [.. __result, .. customSharedPools];
    }

    public static void Register(CustomPotionPoolModel pool)
    {
        customSharedPools.Add(pool);
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
class ActModelGenerateRoomsPatch
{
    [HarmonyPostfix]
    static void ForceAncientToSpawn(ActModel __instance)
    {
        var rooms = Traverse.Create(__instance).Field<RoomSet>("_rooms").Value;
        var rngChosenAncient = rooms.Ancient;
        var ancientToSpawn = CustomContentDictionary.CustomAncients.Find(a => a.ShouldForceSpawn(__instance, rngChosenAncient));

        if (ancientToSpawn != null)
        {
            rooms.Ancient = ancientToSpawn;
        }
    }
}
/*
class CardPoolPatch
{
    internal static void Patch(Harmony harmony)
    {
        Type[] poolTypes = 
            [typeof(IroncladCardPool),
            typeof(SilentCardPool),
            typeof(DefectCardPool),
            typeof(RegentCardPool),
            typeof(NecrobinderCardPool),
            typeof(ColorlessCardPool),
            typeof(TokenCardPool),
            typeof(EventCardPool),
            typeof(QuestCardPool),
            typeof(StatusCardPool),
            typeof(CurseCardPool)
        ]; //CustomCardPoolModel generate method utilizes CustomContentDictionary

        foreach (var poolType in poolTypes)
        {
            var originalMethod = AccessTools.Method(poolType, "GenerateAllCards");
            var postfix = AccessTools.Method(typeof(CardPoolPatch), nameof(AdjustPool));
            harmony.Patch(originalMethod, postfix: new HarmonyMethod(postfix));
        }
    }

    static CardModel[] AdjustPool(CardModel[] __result, CardPoolModel __instance)
    {
        if (CustomContentDictionary.Cards.TryGetValue(__instance.GetType(), out var cards))
        {
            return [.. __result, .. cards];
        }
        return __result;
    }
}
class RelicPoolPatch
{
    internal static void Patch(Harmony harmony)
    {
        Type[] poolTypes =
            [typeof(SharedRelicPool),
            typeof(IroncladRelicPool),
            typeof(SilentRelicPool),
            typeof(DefectRelicPool),
            typeof(RegentRelicPool),
            typeof(NecrobinderRelicPool),
            typeof(EventRelicPool)
        ]; //CustomRelicPoolModel generate method utilizes CustomContentDictionary

        foreach (var poolType in poolTypes)
        {
            var originalMethod = AccessTools.Method(poolType, "GenerateAllRelics");
            var postfix = AccessTools.Method(typeof(RelicPoolPatch), nameof(AdjustPool));
            harmony.Patch(originalMethod, postfix: new HarmonyMethod(postfix));
        }
    }

    static IEnumerable<RelicModel> AdjustPool(IEnumerable<RelicModel> __result, RelicPoolModel __instance)
    {
        if (CustomContentDictionary.Relics.TryGetValue(__instance.GetType(), out var relics))
        {
            return [.. __result, .. relics];
        }
        return __result;
    }
}
class PotionPoolPatch
{
    internal static void Patch(Harmony harmony)
    {
        Type[] poolTypes =
            [typeof(SharedPotionPool),
            typeof(IroncladPotionPool),
            typeof(SilentPotionPool),
            typeof(DefectPotionPool),
            typeof(RegentPotionPool),
            typeof(NecrobinderPotionPool),
            typeof(EventPotionPool),
            typeof(TokenPotionPool)
        ]; //CustomPotionPoolModel generate method utilizes CustomContentDictionary

        foreach (var poolType in poolTypes)
        {
            var originalMethod = AccessTools.Method(poolType, "GenerateAllPotions");
            var postfix = AccessTools.Method(typeof(PotionPoolPatch), nameof(AdjustPool));
            harmony.Patch(originalMethod, postfix: new HarmonyMethod(postfix));
        }
    }

    static IEnumerable<PotionModel> AdjustPool(IEnumerable<PotionModel> __result, PotionPoolModel __instance)
    {
        if (CustomContentDictionary.Potions.TryGetValue(__instance.GetType(), out var potions))
        {
            return [.. __result, .. potions];
        }
        return __result;
    }
}*/