using Assets.Scripts.Unity;
using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;
using HarmonyLib;
using Assets.Scripts.Unity.UI_New.InGame;
using Assets.Scripts.Simulation.Bloons;
using UnityEngine;
using System.Linq;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Simulation.Display;
using MelonLoader;

namespace Combloonation
{
    class DisplayTest
    {
        static Dictionary<string, Color> colors = new Dictionary<string, Color>()
        {
            { "red", new Color(1f, 0f, 0f) },
            { "blue", new Color(1f, 0f, 0f) },
            { "green", new Color(1f, 0f, 0f) },
            { "yellow", new Color(1f, 0f, 0f) },
            { "pink", new Color(1f, 0f, 0f) },
            { "white", new Color(1f, 0f, 0f) },
        };

        public static void SetBloonAppearance(Bloon bloon)
        {
            MelonLogger.Msg("Setting Appearance: " + bloon.bloonModel.id + " " + bloon.Id);
            DisplayNode node = null;
            try
            {
                node = bloon.Node;
            }
            catch (UnhollowerBaseLib.Il2CppException e)
            {
                MelonLogger.Msg(e.Message.TrimEnd());
            }
            if (node == null) { MelonLogger.Msg("Null: Node"); return; }
            var graphic = node.graphic;
            if (graphic == null) { MelonLogger.Msg("Null: graphic"); return; }
            graphic.Scale *= 3;
            var gameObject = graphic.gameObject;
            if (gameObject == null) { MelonLogger.Msg("Null: gameObject"); return; }
            if (!bloon.bloonModel.isMoab)
            {
                gameObject.GetComponent<SpriteRenderer>().color = new Color(.5f, .5f, .5f);
            }
        }

        [HarmonyPatch(typeof(Bloon), nameof(Bloon.Initialise))]
        class BloonInitialize_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Bloon __instance)
            {
                SetBloonAppearance(__instance);
            }
        }
    }
}
