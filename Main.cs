using MelonLoader;
using HarmonyLib;
using BTD_Mod_Helper;
using Assets.Scripts.Unity.UI_New.InGame;
using System.Linq;
using Assets.Scripts.Unity.UI_New.InGame.BloonMenu;
using Il2CppSystem;
using BTD_Mod_Helper.Extensions;
using Assets.Scripts.Models.Rounds;
using static Combloonation.Helpers;
using static Combloonation.Display;
using static Combloonation.Labloontory;
using System.Reflection;
using UnityEngine;

[assembly: MelonInfo(typeof(Combloonation.Main), "Combloonation", "0-beta-r0", "MagicGonads")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
namespace Combloonation
{
    public class Main : BloonsTD6Mod
    {

        //public static string folderPath;
        public static int seed = 2000;
        public static System.Random random;
        public static IDirector director = null;
        public static MethodInfo optional_HelpfulAdditions_AddCustomBloon = null;
        public static int maxFusands = int.MaxValue;

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            MelonLogger.Msg("Combloonations loaded!");
            //folderPath = FileIOUtil.GetSandboxPath() + "/Combloonation";
            //MelonLogger.Msg("Dumping at " + folderPath);
            //Directory.CreateDirectory(folderPath);
            random = new System.Random(seed);
        }

        public override void OnApplicationLateStart()
        {
            Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            Assembly helpfulAdditions = assemblies.FirstOrDefault(assembly => assembly.GetName().Name.Equals("Helpful Additions"));
            if (helpfulAdditions is null) return;
            System.Type mod = helpfulAdditions.GetType("HelpfulAdditions.Mod");
            optional_HelpfulAdditions_AddCustomBloon = mod.GetMethod("AddCustomBloon", new System.Type[] {
                typeof(string), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Vector2?) });
        }

        public override void OnTitleScreen()
        {
            var game = GetGameModel();
            director = new MainDirector(game, seed);
            MelonLogger.Msg("Mutating rounds...");
            director.Mutate();
            MelonLogger.Msg("Finished mutating rounds!");
            //MelonLogger.Msg(string.Join("\n",game.freeplayGroups.OrderBy(f => f.CalculateScore(game)).Select(f => $"${f.CalculateScore(game)}: {f.group.count} x {f.group.bloon} ~> [{f.group.start},{f.group.end}] | {string.Join(", ", f.bounds.Select(b => $"[{b.lowerBounds},{b.upperBounds}]"))}")));
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
                MelonLogger.Msg("Spawning " + DebugString(__instance.model.name));
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
}
