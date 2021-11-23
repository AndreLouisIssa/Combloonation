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
        public static readonly HashSet<int> skipExtra = new HashSet<int>();

        public static void SetBloonAppearance(Bloon bloon, DisplayBehavior display, DisplayNode node, UnityDisplayNode graphic, GameObject gameObject)
        {
            //graphic.Scale *= 3;
            if (!bloon.bloonModel.isMoab)
            {
                gameObject.GetComponent<SpriteRenderer>().color = new Color(.5f, .5f, .5f);
            }
            else
            {
                gameObject.GetComponent<MeshRenderer>().material.color = new Color(.5f, .5f, .5f);
            }
        }
        public static void SetBloonAppearanceA(Bloon bloon)
        {
            MelonLogger.Msg("bloon:" + bloon.bloonModel.id + " " + bloon.Id);
            var display = bloon.display;
            if (display == null) { MelonLogger.Msg("! display"); return; }
            bloonCache[display.entity.Id] = bloon;
            MelonLogger.Msg($"  display: {display.Id} {display.entity.Id}");
            var node = display.node;
            if (node == null) { MelonLogger.Msg("! node"); return; }
            MelonLogger.Msg($"  node: {node.objectId} {node.groupId}");
            var graphic = node.graphic;
            if (graphic == null) { MelonLogger.Msg("! graphic"); return; }
            MelonLogger.Msg($"  graphic: {graphic.name} {graphic.GetInstanceID()}");
            var gameObject = graphic.gameObject;
            if (gameObject == null) { MelonLogger.Msg("! gameObject"); return; }
            MelonLogger.Msg($"  gameObject: {gameObject.name} {gameObject.GetInstanceID()}");

            SetBloonAppearance(bloon, display, node, graphic, gameObject);
        }
        public static void SetBloonAppearanceB(Bloon bloon)
        {
            if (skipExtra.Contains(bloon.Id)) return;
            MelonLogger.Msg("bloon:" + bloon.bloonModel.id + " " + bloon.Id);
            var display = bloon.display;
            if (display == null) { MelonLogger.Msg("! display"); return; }
            MelonLogger.Msg($"  display: {display.Id} {display.entity.Id}");
            var node = display.node;
            if (node == null) { MelonLogger.Msg("! node"); return; }
            MelonLogger.Msg($"  node: {node.objectId} {node.groupId}");
            var graphic = node.graphic;
            if (graphic == null) { MelonLogger.Msg("! graphic"); return; }
            MelonLogger.Msg($"  graphic: {graphic.name} {graphic.GetInstanceID()}");
            var gameObject = graphic.gameObject;
            if (gameObject == null) { MelonLogger.Msg("! gameObject"); return; }
            MelonLogger.Msg($"  gameObject: {gameObject.name} {gameObject.GetInstanceID()}");
            skipExtra.Add(bloon.Id);

            SetBloonAppearance(bloon, display, node, graphic, gameObject);
        }

        [HarmonyPatch(typeof(DisplayNode), nameof(DisplayNode.Graphic), MethodType.Setter)]
        class Patch_DisplayNode_set_Graphic
        {
            [HarmonyPostfix]
            public static void Postfix(DisplayNode __instance)
            {
                bloonCache.TryGetValue(__instance.groupId, out var bloon);
                if (bloon != null) SetBloonAppearanceA(bloon);
            }
        }

        [HarmonyPatch(typeof(Bloon), nameof(Bloon.OnSpawn))]
        class Patch_Bloon_OnSpawn
        {
            [HarmonyPostfix]
            public static void Postfix(Bloon __instance)
            {
                SetBloonAppearanceA(__instance);
            }
        }

        [HarmonyPatch(typeof(InGame), nameof(InGame.Update))]
        class Patch_InGame_Update
        {
            [HarmonyPostfix]
            public static void Postfix(InGame __instance)
            {
                foreach (var bloonSim in __instance.bridge.GetAllBloons())
                {
                    SetBloonAppearanceB(bloonSim.GetBloon());
                }
            }
        }
    }
}
