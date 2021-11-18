using MelonLoader;
using HarmonyLib;
using Assets.Main.Scenes;
using BTD_Mod_Helper;

[assembly: MelonInfo(typeof(Combloonation.Main), "Combloonation", "beta-r0", "MagicGonads")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
namespace Combloonation
{
    public class Main : BloonsMod
    {

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            MelonLogger.Msg("Combloonations loaded!");
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
    }
}
