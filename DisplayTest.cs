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
using Assets.Scripts.Unity.Display;
using System.Collections;
using Assets.Scripts.Simulation.Behaviors;

namespace Combloonation
{
    class DisplayTest
    {
        static IEnumerable<Bloon> toModify = new Queue<Bloon>();

        static Dictionary<string, Color> colors = new Dictionary<string, Color>()
        {
            { "red", new Color(1f, 0f, 0f) },
            { "blue", new Color(1f, 0f, 0f) },
            { "green", new Color(1f, 0f, 0f) },
            { "yellow", new Color(1f, 0f, 0f) },
            { "pink", new Color(1f, 0f, 0f) },
            { "white", new Color(1f, 0f, 0f) },
        };

        public static bool SetBloonAppearance(Bloon bloon)
        {
            MelonLogger.Msg("Setting Appearance: " + bloon.bloonModel.id + " " + bloon.Id);
            var display = bloon.display;
            if (display == null) { MelonLogger.Msg("Null: display"); return false; }
            var node = display.node;
            if (node == null) { MelonLogger.Msg("Null: node"); return false; }
            var graphic = node.graphic;
            if (graphic == null) { MelonLogger.Msg("Null: graphic"); return false; }
            graphic.Scale *= 3;
            var gameObject = graphic.gameObject;
            if (gameObject == null) { MelonLogger.Msg("Null: gameObject"); return false; }
            if (!bloon.bloonModel.isMoab)
            {
                gameObject.GetComponent<SpriteRenderer>().color = new Color(.5f, .5f, .5f);
            }
            return true;
        }

        [HarmonyPatch(typeof(Bloon), nameof(Bloon.Initialise))]
        class Patch_Bloon_Initalise
        {
            [HarmonyPostfix]
            public static void Postfix(Bloon __instance)
            {
                toModify = toModify.Append(__instance);
            }
        }

        [HarmonyPatch(typeof(Bloon), nameof(Bloon.ClearCreatedChildren))]
        class Patch_Bloon_ClearCreatedChildren
        {
            [HarmonyPostfix]
            public static void Postfix(Bloon __instance)
            {
                toModify = toModify.Concat(__instance.childrenCreatedOut.ToArray());
            }
        }

        public static void OnUpdate()
        {
            var toRemove = new Queue<Bloon>();
            foreach (var bloon in toModify)
            {
                if (SetBloonAppearance(bloon)) toRemove.Enqueue(bloon);
            }
            toModify = toModify.Except(toRemove);
        }

    }
}
