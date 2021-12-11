﻿using MelonLoader;
using HarmonyLib;
using Assets.Main.Scenes;
using BTD_Mod_Helper;
using Assets.Scripts.Utils;
using System.IO;
using Assets.Scripts.Unity.UI_New.InGame;
using System.Linq;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.UI_New.InGame.BloonMenu;
using System.Collections.Generic;
using Assets.Scripts.Models.Bloons;
using Assets.Scripts.Simulation.Bloons;
using Il2CppSystem;
using BTD_Mod_Helper.Extensions;
using Assets.Scripts.Models.Rounds;
using static Combloonation.Labloontory;
using static Combloonation.Helpers;
using Assets.Scripts.Models;

[assembly: MelonInfo(typeof(Combloonation.Main), "Combloonation", "0-beta-r0", "MagicGonads")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
namespace Combloonation
{
    public class Main : BloonsTD6Mod
    {

        public static string folderPath;
        public static int seed = 2000;
        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            MelonLogger.Msg("Combloonations loaded!");
            folderPath = FileIOUtil.GetSandboxPath() + "/Combloonation";
            MelonLogger.Msg("Dumping at " + folderPath);
            Directory.CreateDirectory(folderPath);
        }

        public override void OnTitleScreen()
        {
            var game = GetGameModel();
            new RoundMutatorDirector(seed).Produce(game, null);
            //var _director = new RandomDirector(seed);
            //var models = _director.Sort(_director.Produce<BloonModel>(null, 25));
            //foreach (var pair in models)
            //{
            //    MelonLogger.Msg($"{((Model)pair.Value).name} : {pair.Key}");
            //}
            //Fuse(models.Values.Cast<BloonModel>());
        }

        [HarmonyPatch(typeof(InGame), nameof(InGame.Update))]
        class Patch_InGame_Update
        {
            [HarmonyPostfix]
            public static void Postfix(InGame __instance)
            {
                if (__instance.bridge == null) return;
                DisplaySystem.OnInGameUpdate(__instance);
            }

            [HarmonyFinalizer]
            public static Exception Finalizer()
            {
                return null;
            }
        }

        /*
        [HarmonyPatch(typeof(CosmeticHelper), nameof(CosmeticHelper.GetBloonModel))]
        public class Patch_CosmeticHelper_GetBloonModel
        {
            [HarmonyFinalizer]
            public static Exception Finalizer()
            {
                return null;
            }
        }

        [HarmonyPatch(typeof(BloonMenu), nameof(BloonMenu.SortBloons))]
        public class Patch_BloonMenu_SortBloons
        {
            [HarmonyFinalizer]
            public static Exception Finalizer(Exception __exception)
            {
                if (__exception != null)
                {
                    MelonLogger.Msg(__exception.Message);
                }

                return null;
            }
        }
        */

    }
}
