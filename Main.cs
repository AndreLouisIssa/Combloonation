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

[assembly: MelonInfo(typeof(Combloonation.Main), "Combloonation", "0-beta-r0", "MagicGonads")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
namespace Combloonation
{
    public class Main : BloonsMod
    {

        public static string folderPath;
        public static int inGameId = 0;
        public static bool needToRegister = false;
        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            MelonLogger.Msg("Combloonations loaded!");
            folderPath = FileIOUtil.GetSandboxPath() + "/Combloonation";
            MelonLogger.Msg("Dumping at " + folderPath);
            Directory.CreateDirectory(folderPath);
        }

        [HarmonyPatch(typeof(TitleScreen), "Start")]
        public class Initiate
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                Labloontory.MutateRounds();
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
                    needToRegister = true;
                    Labloontory.RefreshRegistered();
                    MelonLogger.Msg("Begun registering");

                }

                //Always run
                if (needToRegister)
                {
                    var bloon = Labloontory.Register();
                    if (bloon == null)
                    {
                        needToRegister = false;
                        MelonLogger.Msg("Stopped registering");
                    }
                }
                
                DisplaySystem.OnInGameUpdate(__instance);
            }
        }

    }
}
