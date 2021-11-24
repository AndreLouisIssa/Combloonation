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
using Assets.Scripts.Simulation.Behaviors;

namespace Combloonation
{
    public static class DisplayTest
    {
        public static readonly Dictionary<int, Bloon> bloonCache = new Dictionary<int, Bloon>();
        public static readonly HashSet<int> skipExtra = new HashSet<int>();

        public static void SetBloonAppearance(Bloon bloon, DisplayBehavior display, DisplayNode node, UnityDisplayNode graphic)
        {
            MelonLogger.Msg("bloon: " + bloon.bloonModel.id + " " + bloon.Id);
            var sprite = graphic.sprite;
            if (sprite != null)
            {
                var s = sprite.sprite;
                var t = s.texture.Duplicate();
                foreach (var xy in t.GetEnumerator())
                {
                    var x = xy.Item1; var y = xy.Item2;
                    var c = t.GetPixel(x, y);
                    c.r = c.g = c.b = new float[] { c.r, c.g, c.b }.Max();
                    t.SetPixel(x, y, c);
                }
                s.SetPropertyValue("texture", t);
                //s.SetTexture(t);
            }
            else foreach (var renderer in graphic.genericRenderers)
            {
                var texture = renderer.material.mainTexture;
                Texture2D texture2D = new Texture2D(texture.width, texture.height);
                texture2D.LoadFromFile($"Test/Desaturated/{bloon.bloonModel.id.ToString().Replace("Fortified","")}.png");
                renderer.material.mainTexture = texture2D;
            }
        }
        public static void SetBloonAppearanceA(Bloon bloon)
        {
            //MelonLogger.Msg("bloon:" + bloon.bloonModel.id + " " + bloon.Id);
            var display = bloon.display;
            if (display == null) return;//{ MelonLogger.Msg("! display"); return; }
            bloonCache[display.entity.Id] = bloon;
            //MelonLogger.Msg($"  display: {display.Id} {display.entity.Id}");
            var node = display.node;
            if (node == null) return;//{ MelonLogger.Msg("! node"); return; }
            //MelonLogger.Msg($"  node: {node.objectId} {node.groupId}");
            var graphic = node.graphic;
            if (graphic == null) return;//{ MelonLogger.Msg("! graphic"); return; }
            //MelonLogger.Msg($"  graphic: {graphic.name} {graphic.GetInstanceID()}");
            skipExtra.Add(bloon.Id);

            SetBloonAppearance(bloon, display, node, graphic);
        }
        public static void SetBloonAppearanceB(Bloon bloon)
        {
            if (skipExtra.Contains(bloon.Id)) return;
            //MelonLogger.Msg("bloon:" + bloon.bloonModel.id + " " + bloon.Id);
            var display = bloon.display;
            if (display == null) return;//{ MelonLogger.Msg("! display"); return; }
            //MelonLogger.Msg($"  display: {display.Id} {display.entity.Id}");
            var node = display.node;
            if (node == null) return;//{ MelonLogger.Msg("! node"); return; }
            //MelonLogger.Msg($"  node: {node.objectId} {node.groupId}");
            var graphic = node.graphic;
            if (graphic == null) return;//{ MelonLogger.Msg("! graphic"); return; }
            //MelonLogger.Msg($"  graphic: {graphic.name} {graphic.GetInstanceID()}");
            skipExtra.Add(bloon.Id);

            SetBloonAppearance(bloon, display, node, graphic);
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
                if (__instance.bridge == null) return;
                Il2CppSystem.Collections.Generic.List<BloonToSimulation> bloonSims;
                try
                {
                    bloonSims = __instance.bridge.GetAllBloons();
                }
                catch
                {
                    return;
                }
                foreach (var bloonSim in bloonSims)
                {
                    SetBloonAppearanceB(bloonSim.GetBloon());
                }
            }
        }
    }
}
