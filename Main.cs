global using System.Linq;

global using BTD_Mod_Helper.Extensions;
using MelonLoader;
using BTD_Mod_Helper;
using Combloonation;

using HarmonyLib;

using System.Reflection;

using Il2CppAssets.Scripts.Models.Rounds;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.BloonMenu;

using static Combloonation.Display;
using static Combloonation.Helpers;
using static Combloonation.Labloontory;

[assembly: MelonInfo(typeof(Combloonation.Main), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace Combloonation;

public class Main : BloonsTD6Mod
{

    public static readonly int seed = 2000;
    public static readonly int maxFusands = int.MaxValue;

    public static readonly System.Random random = new System.Random(seed);
    private static IDirector? director = null;

    public static void Log(string message)
    {
        ModHelper.Msg<Main>(message);
    }

    public override void OnTitleScreen()
    {
        director = new MainDirector(seed);
        Log("Mutating rounds...");
        director.Mutate();
        Log("Finished mutating rounds!");
    }

    [HarmonyPatch(typeof(InGame), nameof(InGame.Update))]
    class Patch_InGame_Update
    {
        [HarmonyPostfix]
        public static void Postfix(InGame __instance)
        {
            InGameUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(SpawnBloonButton), nameof(SpawnBloonButton.UpdateIcon))]
    public class Patch_SpawnBloonButton_UpdateIcon
    {
        [HarmonyPostfix]
        public static void Postfix(SpawnBloonButton __instance)
        {
            SetBloonAppearance(__instance);
        }
    }

    [HarmonyPatch(typeof(SpawnBloonButton), nameof(SpawnBloonButton.SpawnBloon))]
    public class Patch_SpawnBloonButton_SpawnBloon
    {
        [HarmonyPostfix]
        public static void Postfix(SpawnBloonButton __instance)
        {
            Log("Spawning " + DebugString(__instance.model.name));
        }
    }

    [HarmonyPatch(typeof(FreeplayBloonGroupModel), nameof(FreeplayBloonGroupModel.CalculateScore))]
    public class Patch_SpawnBloonButton_CalculateScore
    {
        [HarmonyPostfix]
        public static void Postfix(FreeplayBloonGroupModel __instance, ref float __result)
        {
            __result = director?.Score(__instance) ?? __result;
        }
    }

    [HarmonyPatch(typeof(BloonMenu), nameof(BloonMenu.ToggleFortified))]
    public class Patch_BloonMenu_ToggleFortified { [HarmonyPrefix] public static bool Prefix() => patchingIcons = patchedIcons; }
    [HarmonyPatch(typeof(BloonMenu), nameof(BloonMenu.ToggleCamo))]
    public class Patch_BloonMenu_ToggleCamo { [HarmonyPrefix] public static bool Prefix() => patchingIcons = patchedIcons; }
    [HarmonyPatch(typeof(BloonMenu), nameof(BloonMenu.ToggleRegen))]
    public class Patch_BloonMenu_ToggleRegen { [HarmonyPrefix] public static bool Prefix() => patchingIcons = patchedIcons; }

    [HarmonyPatch(typeof(BloonMenu), nameof(BloonMenu.SortBloons))]
    public class Patch_BloonMenu_SortBloons
    {
        [HarmonyPrefix]
        public static bool Prefix(BloonMenu __instance)
        {
            BloonMenuSortBloons(__instance);
            return false;
        }
    }

}

