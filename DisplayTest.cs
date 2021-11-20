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
using Assets.Scripts.Utils;
using System.Reflection;
using Assets.Scripts.Models.Rounds;
using UnhollowerBaseLib;
using Assets.Scripts.Simulation.Bloons.Behaviors;
using Color = UnityEngine.Color;

namespace Combloonation
{
    public class DisplayTest
    {
        public static readonly Dictionary<int, Bloon> bloonCache = new Dictionary<int, Bloon>();

        public static readonly Dictionary<string, Color> colors = new Dictionary<string, Color>()
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
            MelonLogger.Msg("bloon:" + bloon.bloonModel.id + " " + bloon.Id);
            var display = bloon.display;
            if (display == null) { MelonLogger.Msg("! display"); return false; }
            bloonCache[display.entity.Id] = bloon;
            MelonLogger.Msg($"  display: {display.Id} {display.entity.Id}");
            var node = display.node;
            if (node == null) { MelonLogger.Msg("! node"); return false; }
            MelonLogger.Msg($"  node: {node.objectId} {node.groupId}");
            var graphic = node.graphic;
            if (graphic == null) { MelonLogger.Msg("! graphic"); return false; }
            MelonLogger.Msg($"  graphic: {graphic.name} {graphic.GetInstanceID()}");
            graphic.Scale *= 3;
            var gameObject = graphic.gameObject;
            if (gameObject == null) { MelonLogger.Msg("! gameObject"); return false; }
            MelonLogger.Msg($"  gameObject: {gameObject.name} {gameObject.GetInstanceID()}");
            if (!bloon.bloonModel.isMoab)
            {
                gameObject.GetComponent<SpriteRenderer>().color = new Color(.5f, .5f, .5f);
            }
            return true;
        }

        [HarmonyPatch(typeof(DisplayNode), nameof(DisplayNode.Graphic), MethodType.Setter)]
        class Patch_DisplayNode_set_Graphic
        {
            [HarmonyPostfix]
            public static void Postfix(DisplayNode __instance)
            {
                bloonCache.TryGetValue(__instance.groupId, out var bloon);
                if (bloon != null) SetBloonAppearance(bloon);
            }
        }

        [HarmonyPatch(typeof(Bloon), nameof(Bloon.OnDestroy))]
        class Patch_Bloon_OnDestroy
        {
            [HarmonyPostfix]
            public static void Postfix(Bloon __instance)
            {
                bloonCache.Remove(__instance.display.entity.Id);
            }
        }

        [HarmonyPatch(typeof(Bloon), nameof(Bloon.OnSpawn))]
        class Patch_Bloon_OnSpawn
        {
            [HarmonyPostfix]
            public static void Postfix(Bloon __instance)
            {
                SetBloonAppearance(__instance);
            }
        }

    }
}
