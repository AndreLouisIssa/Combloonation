using Assets.Scripts.Unity;
using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;
using HarmonyLib;
using Assets.Scripts.Unity.UI_New.InGame;
using Assets.Scripts.Simulation.Bloons;
using UnityEngine;
using System.Linq;
using Assets.Scripts.Unity.Bridge;
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
            if (bloon.Node.graphic?.gameObject != null && !bloon.bloonModel.isMoab)
            {
                var gameObject = bloon.Node.graphic?.gameObject;
                gameObject.GetComponent<SpriteRenderer>().color = new Color(.5f, .5f, .5f);
            }
        }

        [HarmonyPatch(typeof(Bloon), nameof(Bloon.UpdateDisplay))]
        class BloonInitialize_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Bloon __instance)
            {
                MelonLogger.Msg("TEST " + __instance.Id);
                SetBloonAppearance(__instance);
                //foreach (var bloon in InGame.Bridge.GetAllBloons().ToList().Select(b => b.GetBloon()))
                //{
                //    SetBloonAppearance(bloon);
                //}
            }
        }
    }
}
