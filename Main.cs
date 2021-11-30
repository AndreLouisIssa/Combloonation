using MelonLoader;
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

[assembly: MelonInfo(typeof(Combloonation.Main), "Combloonation", "0-beta-r0", "MagicGonads")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
namespace Combloonation
{
    public class Main : BloonsMod
    {

        public static string folderPath;
        public static int inGameId = 0;
        public static SeededDirector director = new SeededDirector(2000);
        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            MelonLogger.Msg("Combloonations loaded!");
            folderPath = FileIOUtil.GetSandboxPath() + "/Combloonation";
            MelonLogger.Msg("Dumping at " + folderPath);
            Directory.CreateDirectory(folderPath);
        }

        [HarmonyPatch(typeof(TitleScreen), nameof(TitleScreen.Start))]
        public class Initiate
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                director.MutateRounds();
            }
        }

        [HarmonyPatch(typeof(InGame), nameof(InGame.Update))]
        class Patch_InGame_Update
        {
            [HarmonyPostfix]
            public static void Postfix(InGame __instance)
            {
                if (__instance.bridge == null) return;
                var id = __instance.GameId;
                if (id == 0) return;

                //Run once
                if (id != inGameId)
                {
                    inGameId = id;
                    MelonLogger.Msg("New game!");
                }

                DisplaySystem.OnInGameUpdate(__instance);
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

    }
}
