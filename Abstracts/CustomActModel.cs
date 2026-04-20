using BaseLib.Acts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace BaseLib.Abstracts;

public abstract class CustomActModel : ActModel, ICustomModel
{
    
    #region default values
    
    public override Color MapTraveledColor => new Color("27221C");
    public override Color MapUntraveledColor => new Color("6E7750");
    public override Color MapBgColor => new Color("9B9562");

    public override string[] BgMusicOptions => ["event:/music/act3_a1_v2", "event:/music/act3_a2_v2"];
    public override string[] MusicBankPaths => ["res://banks/desktop/act3_a1.bank", "res://banks/desktop/act3_a2.bank"];
    public override string AmbientSfx => "event:/sfx/ambience/act3_ambience";

    protected override int BaseNumberOfRooms => 15;
    
    public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_3_skel_data.tres";
    public override string ChestSpineSkinNameNormal => "act3";
    public override string ChestSpineSkinNameStroke => "act3_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act3";
    
    #endregion default values

    /// <summary>
    /// Override this if you want to provide your own BackgroundScene
    /// </summary>
    protected virtual string CustomBackgroundScenePath => "res://BaseLib/scenes/dynamic_background.tscn";
    protected abstract string CustomMapTopBgPath { get; }
    protected abstract string CustomMapMidBgPath { get; }
    protected abstract string CustomMapBotBgPath { get; }
    protected abstract string CustomRestSiteBackgroundPath { get; }

    /// <summary>
    /// Override this if you want to replace the chest-visuals in Treasure Rooms.<br></br>
    /// The scenes root node <b>must</b> have a script attached that derives from <see cref="NCustomTreasureRoomChest"/> <br></br>
    /// </summary>
    public virtual string? CustomChestScene => null;
    
    protected virtual BackgroundAssets CustomGenerateBackgroundAssets(Rng rng)
    {
        return  new BackgroundAssets("glory", Rng.Chaotic);
    }
    
    // Must be overriden as its abstract, even if you don't need it
    // Only used in Overgrowth specifically for the very first run, so just override it here with nothing
    protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState) { }
    
    
    #region Patches
    
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.BackgroundScenePath), MethodType.Getter)]
    class CustomActBackgroundScenePath
    {
        [HarmonyPrefix]
        static bool UseAltTexture(ActModel __instance, ref string? __result)
        {
            if (__instance is not CustomActModel customAct) return true;
            __result = customAct.CustomBackgroundScenePath;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.MapTopBgPath), MethodType.Getter)]
    class CustomActMapTopBgPath
    {
        [HarmonyPrefix]
        static bool UseAltTexture(ActModel __instance, ref string? __result)
        {
            if (__instance is not CustomActModel customAct) return true;
            __result = customAct.CustomMapTopBgPath;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.MapMidBgPath), MethodType.Getter)]
    class CustomActMapMidBgPath
    {
        [HarmonyPrefix]
        static bool UseAltTexture(ActModel __instance, ref string? __result)
        {
            if (__instance is not CustomActModel customAct) return true;
            __result = customAct.CustomMapMidBgPath;
            return false;
        }
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.MapBotBgPath), MethodType.Getter)]
    class CustomActMapBotBgPath
    {
        [HarmonyPrefix]
        static bool UseAltTexture(ActModel __instance, ref string? __result)
        {
            if (__instance is not CustomActModel customAct) return true;
            __result = customAct.CustomMapBotBgPath;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.RestSiteBackgroundPath), MethodType.Getter)]
    class CustomActRestSiteBackgroundPath
    {
        [HarmonyPrefix]
        static bool UseAltTexture(ActModel __instance, ref string? __result)
        {
            if (__instance is not CustomActModel customAct) return true;
            __result = customAct.CustomRestSiteBackgroundPath;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateBackgroundAssets))]
    public class CustomActGenerateBackgroundAssets
    {
        [HarmonyPrefix]
        public static bool UseCustomBackgroundAssets(ActModel __instance, Rng rng, ref BackgroundAssets __result)
        {
            if (__instance is not CustomActModel customAct) return true;
            __result = customAct.CustomGenerateBackgroundAssets(rng);
            return false;
        }
    }
    
    [HarmonyPatch(typeof(NTreasureRoom), nameof(NTreasureRoom._Ready))]
    public static class CustomActTreasureChest
    {
        private static readonly AccessTools.FieldRef<NTreasureRoom, IRunState?> RunStateRef =
                    AccessTools.FieldRefAccess<NTreasureRoom, IRunState?>("_runState");
        private static readonly AccessTools.FieldRef<NTreasureRoom, Node2D?> ChestNodeRef =
                    AccessTools.FieldRefAccess<NTreasureRoom, Node2D?>("_chestNode");
        private static readonly AccessTools.FieldRef<NTreasureRoom, NButton?> ChestButtonRef =
                    AccessTools.FieldRefAccess<NTreasureRoom, NButton?>("_chestButton");

        [HarmonyPostfix]
        public static void InsertCustomChestVisualNode(NTreasureRoom __instance)
        {
            // validation
            IRunState? runState = RunStateRef(__instance);
            if (runState?.Act is not CustomActModel customActModel) return;
            if (customActModel.CustomChestScene is null) return;
            Node2D? chestNode = ChestNodeRef(__instance);
            NButton? chestButton = ChestButtonRef(__instance);
            if (chestNode is null || chestButton is null) // should in theory never be the case
            {
                BaseLibMain.Logger.Warn("References not found. Using normal Chest Visuals instead");
                return;
            }
        
            // node insertion
            chestNode.Visible = false; // Not removed so the game can still access the node whenever it wants, to prevent errors/crashing.
            Node parent = chestNode.GetParent();
            NCustomTreasureRoomChest? customTreasureRoom = NCustomTreasureRoomChest.Create(__instance, runState, chestButton, customActModel.CustomChestScene);
            if (customTreasureRoom is null)
            {
                BaseLibMain.Logger.Error($"Tried to instantiate custom treasure chest node but failed. Scene path: {customActModel.CustomChestScene}");
                return;
            }
            parent.AddChildSafely(customTreasureRoom);
        }
    }
    
    #endregion Patches
    
}

