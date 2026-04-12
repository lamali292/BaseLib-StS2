using System.Runtime.CompilerServices;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Saves;

namespace BaseLib.Abstracts;

public abstract class CustomBadge(SerializableRun run, ulong playerId) : Badge(run, playerId)
{
    public override string Id => GetType().GetPrefix() + GetType().Name.ToSnakeCase().ToUpperInvariant();
    public virtual string? CustomBadgeIconPath => null;
    
    
  
}

[HarmonyPatch(typeof(BadgePool), nameof(BadgePool.CreateAll))]
class CustomBadgesPatch
{
    [HarmonyPostfix]
    static IReadOnlyCollection<Badge> AddCustomBadges(IReadOnlyCollection<Badge> __result, SerializableRun run, ulong playerId)
    {
        var list = __result.ToList();
        foreach (var type in CustomContentDictionary.CustomBadgeTypes)
            list.Add((Badge)Activator.CreateInstance(type, run, playerId)!);
        return list;
    }
}


[HarmonyPatch(typeof(AssetCache), "LoadAsset")]
class AssetCacheLoadAssetPatch
{
    static bool Prefix(string path, ref Resource __result)
    {
        if (ResourceLoader.Exists(path)) return true;
        if (!path.Contains("game_over_screen/badge_")) return true;
        // Return placeholder for missing custom badge icons
        __result = ResourceLoader.Load<Resource>(ImageHelper.GetImagePath("debug/placeholder_64.png"));
        return false;
    }
}

[HarmonyPatch(typeof(NBadge), nameof(NBadge.Create), typeof(string), typeof(BadgeRarity))]
class NBadgeCreateStringPatch
{
    
    
    static void Postfix(NBadge __result, string id)
    {
        if (__result == null) return;
        var type = CustomContentDictionary.CustomBadgeTypes
            .FirstOrDefault(t => (t.GetPrefix() + t.Name.ToSnakeCase().ToUpperInvariant())
                .Equals(id, StringComparison.OrdinalIgnoreCase));
        if (type == null) return;
        var badge = (CustomBadge)RuntimeHelpers.GetUninitializedObject(type);
        if (string.IsNullOrEmpty(badge.CustomBadgeIconPath)) return;
        var texture = ResourceLoader.Load<Texture2D>(badge.CustomBadgeIconPath);
        if (texture == null) return;
        __result.GetNode<TextureRect>("%Icon").Texture = texture;
    }
}


[HarmonyPatch(typeof(Badge), "get_BadgeIcon")]
class BadgeIconGetterPatch
{
    static bool Prefix(Badge __instance, ref Texture2D __result)
    {
        if (__instance is not CustomBadge badge) return true;
        if (string.IsNullOrEmpty(badge.CustomBadgeIconPath)) return true;
        __result = ResourceLoader.Load<Texture2D>(badge.CustomBadgeIconPath);
        return false;
    }
}
