﻿using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;
using HarmonyLib;
using Assets.Scripts.Unity.UI_New.InGame;
using Assets.Scripts.Simulation.Bloons;
using System.Linq;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Simulation.Display;
using MelonLoader;
using Assets.Scripts.Unity.Display;
using Assets.Scripts.Models.Bloons;
using System;
using UnityEngine;
using System.IO;
using static Combloonation.Labloontory;
using Vector2 = Assets.Scripts.Simulation.SMath.Vector2;

namespace Combloonation
{
    public static class DisplaySystem
    {
        public static bool enabled = false;
        public static readonly Dictionary<int, Bloon> bloonCache = new Dictionary<int, Bloon>();
        public static readonly HashSet<int> skipExtra = new HashSet<int>();
        public static Dictionary<string, Texture2D> computedTextures = new Dictionary<string, Texture2D>();

        public static Dictionary<string, Color> baseColors = new Dictionary<string, Color>()
        {
            { "Red",     HexColor("ee2020") },
            { "Blue",    HexColor("2f9ae0") },
            { "Green",   HexColor("78a911") },
            { "Yellow",  HexColor("ffd511") },
            { "Pink",    HexColor("f05363") },
            { "White",   HexColor("e7e7e7") },
            { "Black",   HexColor("252525") },
            { "Lead",    HexColor("8d95a7") },
            { "Purple",  HexColor("9326e0") },
            { "Zebra",   HexColor("9f9f9f") },
            { "Rainbow", HexColor("ffac24") },
            { "Ceramic", HexColor("bd6b1c") },
            { "Moab",    HexColor("1d83d9") },
            { "Bfb",     HexColor("ab0000") },
            { "Zomg",    HexColor("cefc02") },
            { "Ddt",     HexColor("454b41") },
            { "Bad",     HexColor("bb00c6") },
        };

        public static Color TintMask(Color tint, Color mask)
        {
            //Color.RGBToHSV(mask, out var mh, out var ms, out var mv);
            //Color.RGBToHSV(tint, out var th, out var ts, out var tv);
            //var col = Color.HSVToRGB(th, ms, mv);
            //col.a = mask.a;
            //return col;
            return new Color(tint.r, tint.g, tint.b, mask.a);
        }
        public static Color HexColor(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

        public static Color AverageColor(Color a, Color b)
        {
            return Color.Lerp(a, b, (1 + b.a - a.a) / 2);
        }

        public static Color AverageColor(IEnumerable<Color> c)
        {
            return c.Aggregate((a, b) => AverageColor(a, b));
        }

        public static Color AverageColor(params Color[] c)
        {
            return AverageColor((IEnumerable<Color>)c);
        }

        public static List<Color> GetBaseColors(this BloonModel bloon)
        {
            var cols = new List<Color>();
            foreach (var id in MinimalBloonIdsFromId(bloon.id))
            {
                var got = baseColors.TryGetValue(id, out var col);
                if (got) cols.Add(col);
                MelonLogger.Msg($"{id} : {(got ? col.ToString() : got.ToString())}");
            }
            return cols;
        }

        public static IEnumerable<Tuple<int, int>> GetEnumerator(this Texture2D texture)
        {
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    yield return new Tuple<int, int>(x, y);
                }
            }
        }

        public static Texture2D Duplicate(this Texture texture, Rect? proj = null)
        {
            if (proj == null)
            {
                proj = new Rect(0, 0, texture.width, texture.height);
            }
            var rect = (Rect)proj;
            texture.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height);
            rt.filterMode = FilterMode.Point;
            RenderTexture.active = rt;
            Graphics.Blit(texture, rt);
            Texture2D texture2 = new Texture2D((int)rect.width, (int)rect.height);
            texture2.ReadPixels(new Rect(rect.x, texture.height - rect.height - rect.y, rect.width, rect.height), 0, 0);
            texture2.Apply();
            RenderTexture.active = null;
            return texture2;
        }

        public static Texture2D ToTexture2D(this Texture texture)
        {
            return (texture is Texture2D t2D) ? t2D : texture.Duplicate();
        }

        public static Texture2D ToReadable(this Texture texture)
        {
            var t2D = texture.ToTexture2D();
            return (t2D.isReadable) ? t2D : t2D.Duplicate();
        }

        public static IEnumerable<Tuple<int, int>> GetEnumerator(this Texture texture)
        {
            return texture.ToTexture2D().GetEnumerator();
        }

        public static Texture2D Duplicate(this Texture texture, Func<int, int, Color, Color> func, Rect? proj = null)
        {
            if (proj == null)
            {
                proj = new Rect(0, 0, texture.width, texture.height);
            }
            var t = texture.Duplicate(proj);
            foreach (var xy in t.GetEnumerator())
            {
                var x = xy.Item1; var y = xy.Item2;
                t.SetPixel(x, y, func(x, y, t.GetPixel(x, y)));
            }
            t.Apply();
            return t;
        }

        public static Texture2D TintMask(this Texture texture, Color tint, Rect? proj = null)
        {
            return texture.Duplicate((x, y, c) => TintMask(tint, c), proj);
        }

        public static Texture2D TintMask(this Texture texture, List<Color> tints, Rect? proj = null)
        {
            if (tints == null) throw new ArgumentNullException(nameof(tints));
            int w = 0; int h = 0;
            if (proj is Rect rect) { w = (int)rect.width; h = (int)rect.height; }
            else { w = texture.width; h = texture.height; }
            var w2 = w / 2; var h2 = h / 2;
            var map = RegionScalarMap.Regions.annular(-w2, w - w2, -h2, h - h2);
            return texture.Duplicate((x, y, c) => TintMask(tints.SplitRange(map, x - w2, y - h2), c), proj);
        }

        public static IEnumerable<Color> GetColorEnumerator(this Texture texture)
        {
            var t2D = texture.ToReadable();
            return t2D.GetEnumerator().Select(t => t2D.GetPixel(t.Item1, t.Item2));
        }

        public static Texture2D GenerateTexture(this Bloon bloon, Texture oldTexture, Rect? proj = null)
        {
            if (bloon == null) throw new ArgumentNullException(nameof(bloon));
            var model = bloon.bloonModel;
            if (oldTexture.isReadable) return null;
            var exists = computedTextures.TryGetValue(model.id, out var texture);
            if (exists) return texture;
            var tints = model.GetBaseColors();
            if (tints.Count == 0) return computedTextures[model.id] = null;
            computedTextures[model.id] = texture = oldTexture.TintMask(tints, proj);
            texture.SaveToPNG($"{Main.folderPath}/{model.id}.png");
            return texture;
        }

        public static void SetBloonAppearance(Bloon bloon, UnityDisplayNode graphic)
        {
            MelonLogger.Msg("bloon: " + bloon.bloonModel.id + " " + bloon.Id);

            var sprite = graphic.sprite;
            if (sprite != null)
            {
                var texture = bloon.GenerateTexture(sprite.sprite.texture, sprite.sprite.textureRect);
                if (texture != null)
                {
                    sprite.sprite = texture.CreateSpriteFromTexture(sprite.sprite.pixelsPerUnit, sprite.sprite.pivot);
                    sprite.material.mainTexture = texture;
                    sprite.sharedMaterial.mainTexture = texture;
                    foreach (var m in sprite.materials.Concat(sprite.sharedMaterials)) m.mainTexture = texture;
                }
            }
            else
            {
                var renderer = graphic.genericRenderers.First(r => r.name == "Body");
                var texture = bloon.GenerateTexture(renderer.material.mainTexture);
                if (texture != null) foreach (var r in graphic.genericRenderers) r.SetMainTexture(texture);
            }
        }

        public static void SetBloonAppearanceA(Bloon bloon)
        {
            var display = bloon.display;
            if (display == null) return;
            bloonCache[display.entity.Id] = bloon;
            var node = display.node;
            if (node == null) return;
            var graphic = node.graphic;
            if (graphic == null) return;
            skipExtra.Add(bloon.Id);
            SetBloonAppearance(bloon, graphic);
        }
        public static void SetBloonAppearanceB(Bloon bloon)
        {
            if (skipExtra.Contains(bloon.Id)) return;
            var display = bloon.display;
            if (display == null) return;
            var node = display.node;
            if (node == null) return;
            var graphic = node.graphic;
            if (graphic == null) return;
            skipExtra.Add(bloon.Id);
            SetBloonAppearance(bloon, graphic);
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
                if (!enabled) return;
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