using BTD_Mod_Helper.Extensions;
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
using static Combloonation.Labloontory;

namespace Combloonation
{
    public static class DisplaySystem
    {
        public static Dictionary<string, Texture2D> computedTextures = new Dictionary<string, Texture2D>();
        public static IOverlay boundaryColor = new ColorOverlay(HexColor("000000"));
        public static IOverlay emptyColor = new DelegateOverlay((c,x,y,r) => c);
        public static Dictionary<string, IOverlay> baseColors = new Dictionary<string, IOverlay>()
        {
            { "Red",     new TintColorOverlay(HexColor("fe2020")) },
            { "Blue",    new TintColorOverlay(HexColor("2f9ae0")) },
            { "Green",   new TintColorOverlay(HexColor("78a911")) },
            { "Yellow",  new TintColorOverlay(HexColor("ffd511")) },
            { "Pink",    new TintColorOverlay(HexColor("f05363")) },
            { "White",   new TintColorOverlay(HexColor("e7e7e7")) },
            { "Black",   new TintColorOverlay(HexColor("252525")) },
            { "Lead",    new TintColorOverlay(HexColor("7d85d7")) },
            { "Purple",  new TintColorOverlay(HexColor("9326e0")) },
            { "Zebra",   new TintColorOverlay(HexColor("bfbfbf")) },
            { "Rainbow", new TintColorOverlay(HexColor("ffac24")) },
            { "Ceramic", new TintColorOverlay(HexColor("bd6b1c")) },
            { "Moab",    new TintColorOverlay(HexColor("1d83d9")) },
            { "Bfb",     new TintColorOverlay(HexColor("ab0000")) },
            { "Zomg",    new TintColorOverlay(HexColor("cefc02")) },
            { "Ddt",     new TintColorOverlay(HexColor("454b41")) },
            { "Bad",     new TintColorOverlay(HexColor("bb00c6")) },
        };

        public interface IOverlay
        {
            Color Pixel(Color c, int x, int y, Rect r);
        }

        public class DelegateOverlay : IOverlay
        {
            public Func<Color, int, int, Rect, Color> func;

            public DelegateOverlay(Func<Color, int, int, Rect, Color> func)
            {
                this.func = func;
            }
            public Color Pixel(Color c, int x, int y, Rect r)
            {
                return func(c, x, y, r);
            }
        }

        public abstract class TintOverlay : IOverlay
        {
            public float interp = 0.8f;
            public abstract Color Tint(Color c, int x, int y, Rect r);
            public Color Pixel(Color c, int x, int y, Rect r)
            {
                var t = Tint(c, x, y, r);
                Color.RGBToHSV(c, out var mh, out var ms, out var mv);
                Color.RGBToHSV(t, out var th, out var ts, out var tv);
                var col = Color.HSVToRGB(th, ms, mv);
                col.a = c.a;
                return Color.Lerp(col, new Color(t.r, t.g, t.b, c.a), interp);
            }
        }

        public class ColorOverlay : IOverlay
        {

            public Color c;
            public ColorOverlay(Color c)
            {
                this.c = c;
            }
            public Color Pixel(Color c, int x, int y, Rect r)
            {
                return new Color(this.c.r, this.c.g, this.c.b, c.a);
            }
        }

        public class TintColorOverlay : TintOverlay
        {
            public Color c;

            public TintColorOverlay(Color c)
            {
                this.c = c;
            }

            public TintColorOverlay(Color c, float t)
            {
                this.c = c; interp = t;
            }

            public override Color Tint(Color c, int x, int y, Rect r)
            {
                return this.c;
            }
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

        public static List<IOverlay> GetBaseColors(this BloonModel bloon)
        {
            var cols = new List<IOverlay> { emptyColor };
            foreach (var id in BaseBloonIdsFromId(bloon.id).Skip(1))
            {
                var got = baseColors.TryGetValue(id, out var col);
                if (got) cols.Add(col);
            }
            return cols;
        }
        public static IOverlay GetPrimaryColor(this BloonModel bloon)
        {
            var primary = BaseBloonIdsFromId(bloon.id).First();
            var got = baseColors.TryGetValue(primary, out var col);
            if (got) return col;
            return emptyColor;

        }

        public static IEnumerable<Tuple<int, int>> GetEnumerator(this Texture2D texture)
        {
            for (int x = 0; x < texture.width; x++) for (int y = 0; y < texture.height; y++)
            {
                yield return new Tuple<int, int>(x, y);
            }
        }

        public static Texture2D Duplicate(this Texture texture, Rect? proj = null)
        {
            if (proj == null) proj = new Rect(0, 0, texture.width, texture.height);
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

        public static Rect RectOrTexture(Texture texture, Rect? proj = null)
        {
            float w; float h; float x; float y;
            if (proj is Rect rect) {
                w = rect.width; h = rect.height;
                x = rect.x; y = rect.y;
            }
            else {
                w = texture.width; h = texture.height;
                x = 0f; y = 0f;
            }
            return new Rect(x, y, w, h);
        }

        public static Texture2D TintMask(this Texture texture, IOverlay tint, Rect? proj = null)
        {
            var rect = RectOrTexture(texture, proj);
            return texture.Duplicate((x, y, c) => tint.Pixel(c, x, y, rect), proj);
        }

        public static Tuple<RegionScalarMap, float, float> GetRegionMap(Texture texture, Rect? proj = null)
        {
            int w; int h;
            if (proj is Rect rect) { w = (int)rect.width; h = (int)rect.height; }
            else { w = texture.width; h = texture.height; }
            var w2 = w / 2; var h2 = h / 2;
            var map = RegionScalarMap.Regions.spiral(1.3f, 0.6f)(-w2, w - w2, -h2, h - h2);
            return new Tuple<RegionScalarMap, float, float>(map, w2, h2);
        }

        public static Texture2D TintMask(this Texture texture, List<IOverlay> tints, Rect? proj = null)
        {
            if (tints == null) throw new ArgumentNullException(nameof(tints));
            var rect = RectOrTexture(texture, proj);
            var map = GetRegionMap(texture, proj);
            return texture.Duplicate((x, y, c) => 
                tints.SplitRange(null, map.Item1, x - map.Item2, y - map.Item3).Pixel(c, x, y, rect), proj);
        }

        public static Texture2D TintMask(this Texture texture, BloonModel bloon, Rect? proj = null)
        {
            if (bloon == null) throw new ArgumentNullException(nameof(bloon));
            var rect = RectOrTexture(texture, proj);
            var map = GetRegionMap(texture, proj);
            var ws = BloonsFromBloon(bloon).Select(b => b.danger).ToArray();
            ws[0] *= 1.5f;
            return texture.Duplicate((x, y, c) =>
                GetBaseColors(bloon).SplitRange(ws, true, GetPrimaryColor(bloon), map.Item1, x - map.Item2, y - map.Item3).Pixel(c, x, y, rect), proj);
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
            if (oldTexture == null) return computedTextures[model.id] = null;
            if (oldTexture.isReadable) return null;
            var exists = computedTextures.TryGetValue(model.id, out var texture);
            if (exists) return texture;
            if (!model.id.Contains(delim)) return computedTextures[model.id] = null;
            MelonLogger.Msg("bloon: " + DebugString(model.id) + " " + bloon.Id);
            computedTextures[model.id] = texture = oldTexture.TintMask(model, proj);
            if (texture != null) texture.SaveToPNG($"{Main.folderPath}/{DebugString(model.id)}.png");
            return texture;
        }

        public static void SetBloonAppearance(Bloon bloon, UnityDisplayNode graphic)
        {
            var sprite = graphic.sprite;
            if (sprite != null)
            {
                var texture = bloon.GenerateTexture(sprite.sprite.texture, sprite.sprite.textureRect);
                if (texture != null)
                {
                    sprite.sprite = texture.CreateSpriteFromTexture(sprite.sprite.pixelsPerUnit);
                    sprite.material.mainTexture = texture;
                    sprite.sharedMaterial.mainTexture = texture;
                    foreach (var m in sprite.materials.Concat(sprite.sharedMaterials)) m.mainTexture = texture;
                    foreach (var r in graphic.genericRenderers) r.SetMainTexture(texture);
                }
            }
            else
            {
                var renderer = graphic.genericRenderers.First(r => r.name == "Body");
                var texture = bloon.GenerateTexture(renderer.material.mainTexture);
                if (texture != null) foreach (var r in graphic.genericRenderers) r.SetMainTexture(texture);
            }
        }

        public static void SetBloonAppearance(Bloon bloon)
        {
            var display = bloon.display;
            if (display == null) return;
            var node = display.node;
            if (node == null) return;
            var graphic = node.graphic;
            if (graphic == null) return;
            SetBloonAppearance(bloon, graphic);
        }

        public static void OnInGameUpdate(InGame inGame)
        {
            List<BloonToSimulation> bloonSims;
            try { bloonSims = inGame.bridge.GetAllBloons().ToList(); } catch { return; }
            foreach (var bloonSim in bloonSims) { SetBloonAppearance(bloonSim.GetBloon()); }
        }

    }
}
