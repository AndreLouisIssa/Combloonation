using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;
using Assets.Scripts.Unity.UI_New.InGame;
using Assets.Scripts.Simulation.Bloons;
using System.Linq;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Unity.Display;
using Assets.Scripts.Models.Bloons;
using System;
using UnityEngine;
using static Combloonation.Labloontory;
using static Combloonation.Helpers;
using static Combloonation.RegionScalarMap;
using MelonLoader;
using Mesh = Assets.Scripts.Simulation.Display.Mesh;
using Assets.Scripts.Simulation.Display;
using HarmonyLib;

namespace Combloonation
{
    public static class DisplaySystem
    {
        public static Dictionary<string, Texture2D> computedTextures = new Dictionary<string, Texture2D>();
        public static IOverlay emptyColor = new DelegateOverlay((c, x, y) => c);
        public static IOverlay boundaryColor = emptyColor;
        public static IOverlay fortifiedColorA = new ColorOverlay(HexColor("cd5d10"));
        public static IOverlay fortifiedColorB = new ColorOverlay(HexColor("cecece"));
        public static Tuple<List<IOverlay>, List<float>> fortifiedColors = new Tuple<List<IOverlay>, List<float>>(
            new List<IOverlay>{emptyColor,fortifiedColorB,fortifiedColorA,fortifiedColorB,emptyColor,fortifiedColorB,fortifiedColorA,fortifiedColorB,emptyColor},
            new List<float>{30f,2f,8f,2f,30f,2f,8f,2f,30f});
        public static Dictionary<string, IOverlay> baseColors = new Dictionary<string, IOverlay>()
        {
            { "Red",     new ColorOverlay(HexColor("fe2020")) },
            { "Blue",    new ColorOverlay(HexColor("2f9ae0")) },
            { "Green",   new ColorOverlay(HexColor("78a911")) },
            { "Yellow",  new ColorOverlay(HexColor("ffd511")) },
            { "Pink",    new ColorOverlay(HexColor("f05363")) },
            { "White",   new ColorOverlay(HexColor("e7e7e7")) },
            { "Black",   new ColorOverlay(HexColor("252525")) },
            { "Lead",    new ColorOverlay(HexColor("7d85d7")) },
            { "Purple",  new ColorOverlay(HexColor("9326e0")) },
            { "Zebra",   new ColorOverlay(HexColor("bfbfbf")) },
            { "Rainbow", new ColorOverlay(HexColor("ffac24")) },
            { "Ceramic", new ColorOverlay(HexColor("bd6b1c")) },
            { "Moab",    new ColorOverlay(HexColor("1d83d9")) },
            { "Bfb",     new ColorOverlay(HexColor("ab0000")) },
            { "Zomg",    new ColorOverlay(HexColor("cefc02")) },
            { "Ddt",     new ColorOverlay(HexColor("454b41")) },
            { "Bad",     new ColorOverlay(HexColor("bb00c6")) },
        };

        public interface IOverlay
        {
            Color Pixel(Color c, float x, float y);
        }

        public class DelegateOverlay : IOverlay
        {
            public Func<Color, float, float, Color> func;

            public DelegateOverlay(Func<Color, float, float, Color> func) { this.func = func; }

            public Color Pixel(Color c, float x, float y)
            {
                return func(c, x, y);
            }
        }

        public class PipeOverlay : IOverlay
        {
            public IOverlay a;
            public IOverlay b;

            public PipeOverlay(IOverlay a, IOverlay b)
            {
                this.a = a; this.b = b;
            }
            public Color Pixel(Color c, float x, float y)
            {
                var _c = a.Pixel(c, x, y);
                return b.Pixel(_c, x, y);
            }
        }

        public class RegionOverlay : IOverlay
        {
            public List<IOverlay> cs;
            public List<float> ps;
            public RegionScalarMap map;

            public RegionOverlay(List<IOverlay> cs, List<float> ws, RegionScalarMap map)
            {
                this.cs = cs; ps = WeightsToPivots(ws); this.map = map;
            }

            public Color Pixel(Color c, float x, float y)
            {
                return cs.SplitRange(ps, null, map, x, y).Pixel(c, x, y);
            }
        }

        public class TintOverlay : IOverlay
        {
            public float t = 0.8f;
            public Func<float, float, float> tf;
            public IOverlay c;

            public TintOverlay(IOverlay c) { this.c = c; }
            public TintOverlay(IOverlay c, float t) : this(c) { this.t = t; }
            public TintOverlay(IOverlay c, Func<float, float, float> tf) : this(c) { this.tf = tf; }

            public Color Pixel(Color mc, float x, float y)
            {
                var tp = (tf != null) ? tf(x,y) : t;
                var tc = c.Pixel(mc, x, y);
                return Color.Lerp(mc, new Color(tc.r, tc.g, tc.b, mc.a), tp);
            }
        }

        public class ColorOverlay : IOverlay
        {

            public Color c;
            public ColorOverlay(Color c) { this.c = c; }
            public Color Pixel(Color c, float x, float y)
            {
                return new Color(this.c.r, this.c.g, this.c.b, c.a);
            }
        }

        public class BoundOverlay : IOverlay
        {

            public IOverlay ci;
            public IOverlay co;
            public float b = 1f;
            public Func<float,float,bool> bf;

            public BoundOverlay(IOverlay ci, IOverlay co) { this.ci = ci; this.co = co; }
            public BoundOverlay(IOverlay ci, IOverlay co, float b) : this(ci, co) { this.b = b; }
            public BoundOverlay(IOverlay ci, IOverlay co, Func<float,float,bool> bf) : this(ci, co) { this.bf = bf; }
                            
            public Color Pixel(Color c, float x, float y)
            {
                bool ins;
                if (bf == null) ins = x * x + y * y > b * b;
                else ins = bf(x, y);
                if (ins) return co.Pixel(c, x, y);
                return ci.Pixel(c, x, y);
            }
        }

        public static Color HexColor(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

        public static Color Average(this Color a, Color b)
        {
            return Color.Lerp(a, b, (1 + b.a - a.a) / 2);
        }

        public static Tuple<IOverlay, List<IOverlay>> GetColors(this BloonModel bloon)
        {
            var ids = BaseBloonNamesFromName(bloon.name);
            var primary = ids.First();
            var got = baseColors.TryGetValue(primary, out var pcol);
            if (!got) pcol = emptyColor;
            var cols = new List<IOverlay> { };
            foreach (var id in ids.Skip(1))
            {
                got = baseColors.TryGetValue(id, out var col);
                if (got) cols.Add(col);
            }
            return new Tuple<IOverlay, List<IOverlay>>(pcol, cols);
        }

        public static List<IOverlay> GetSecondaryColors(this BloonModel bloon)
        {
            var cols = new List<IOverlay> { };
            foreach (var id in BaseBloonNamesFromName(bloon.name).Skip(1))
            {
                var got = baseColors.TryGetValue(id, out var col);
                if (got) cols.Add(col);
            }
            return cols;
        }
        public static IOverlay GetPrimaryColor(this BloonModel bloon)
        {
            var primary = BaseBloonNamesFromName(bloon.name).First();
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
            if (proj == null) { proj = new Rect(0, 0, texture.width, texture.height); }
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

        public static Rect GetRegionRect(Texture texture, Rect? proj = null)
        {
            int w; int h;
            if (proj is Rect rect) { w = (int)rect.width; h = (int)rect.height; }
            else { w = texture.width; h = texture.height; }
            var w2 = w / 2; var h2 = h / 2;
            return new Rect(-w2, -h2, w, h);
        }

        public static Texture2D NewMergedTexture(this FusionBloonModel bloon, Texture texture, bool fromMesh, Rect? proj = null)
        {
            if (bloon == null) throw new ArgumentNullException(nameof(bloon));
            var cols = GetColors(bloon);
            if (cols.Item2.Count == 0) return texture.Duplicate(proj);
            var ws = bloon.fusands.Skip(1).Where(b => baseColors.ContainsKey(b.baseId)).Select(b => b.danger).ToList();
            var mrect = GetRegionRect(texture, proj);
            var map = Regions.spiral(1.3f, 0.6f)(mrect.x, mrect.x + mrect.width, mrect.y, mrect.y + mrect.height);
            var r = Math.Min(mrect.width, mrect.height)/2;
            var fbase = bloon.fusands.First();
            r *= ws[0] / fbase.danger;
            var dx = 0f; var dy = 0f;
            if (fromMesh)
            {
                dx = mrect.width * 0.165f;
                dy = mrect.height * 0.1f;
                r *= 0.5f;
            }
            else if (!fbase.isGrow) dy = -mrect.height * 0.05f;
            float r_iob, r_iib, r_oob;
            Func<float, float, float> curve;
            if (!fbase.isGrow && bloon.isGrow)
            {
                curve = (x, y) => (float)HeartCurve(x, y);
                r *= 0.90f;
            }
            else
            {
                curve = (x, y) => (float)CircleCurve(x, y);
                r *= 1.25f;
            }
            var col = emptyColor;
            if (!fbase.isCamo && bloon.isCamo)
            {
                var cmx = 66f / mrect.width; var cmy = 84f / mrect.height;
                col = new PipeOverlay(col,new DelegateOverlay((c,x,y) => { 
                    x *= cmx; y *= cmy;
                    var n1 = Mathf.PerlinNoise(18.05f + x / 31f, 67f + y / 17f);
                    var n2 = Mathf.PerlinNoise(184f + x / 20f,627f + y / 8f);
                    return c.RGBMultiplied((float)Math.Ceiling(4*n1 + 2*n2)/6);
                }));
            }
            if (!fbase.isFortified && bloon.isFortified)
            {
                col = new PipeOverlay(col,new RegionOverlay(fortifiedColors.Item1, fortifiedColors.Item2,
                    Regions.vertical(mrect.x,mrect.x + mrect.width, mrect.y, mrect.y + mrect.height)));
            }
            r_iob = r*0.6f; r_iib = 0.85f*r_iob; r_oob = r_iob * 1.15f;
            Func<float,float,float> tf = (x,y) => (float)TERF(curve(x/r_oob,y/r_oob),1f,-1f);
            var tcols = cols.Item2;
            var dcol = new RegionOverlay(tcols, ws, map);
            var bcol = new BoundOverlay(dcol, boundaryColor, (x, y) => curve(x / r_iib, y / r_iib) >= 0);
            var bbcol = new BoundOverlay(bcol, dcol, (x, y) => curve(x / r_iob, y / r_iob) >= 0);
            var tcol = new TintOverlay(bbcol, tf);
            var bbbcol = new BoundOverlay(tcol, emptyColor, (x, y) => curve(x / r_oob, y / r_oob) >= 0);
            col = new PipeOverlay(col, bbbcol);
            return texture.Duplicate((x, y, c) => col.Pixel(c, x + (int)(dx + mrect.x), y + (int)(dy + mrect.y)), proj);
        }

        public static Texture2D GetMergedTexture(this FusionBloonModel bloon, Texture oldTexture, bool fromMesh, Rect? proj = null)
        {
            if (bloon == null) throw new ArgumentNullException(nameof(bloon));
            if (oldTexture == null) return computedTextures[bloon.name] = null;
            if (oldTexture.isReadable) return null;
            var exists = computedTextures.TryGetValue(bloon.name, out var texture);
            if (exists) return texture;
            computedTextures[bloon.name] = texture = bloon.NewMergedTexture(oldTexture, fromMesh, proj);
            if (texture != null) texture.SaveToPNG($"{Main.folderPath}/{DebugString(bloon.name)}.png");
            return texture;
        }

        public static void SetBloonAppearance(this FusionBloonModel bloon, UnityDisplayNode graphic)
        {
            var sprite = graphic.sprite;
            if (sprite != null)
            {
                var texture = bloon.GetMergedTexture(sprite.sprite.texture, false, sprite.sprite.textureRect);
                if (texture != null)
                {
                    sprite.sprite = texture.CreateSpriteFromTexture(sprite.sprite.pixelsPerUnit);
                }
            }
            else
            {
                var renderer = graphic.genericRenderers.First(r => r.name == "Body");
                MelonLogger.Msg(string.Join(", ", graphic.genericRenderers.Select(r => r.name)));
                var texture = bloon.GetMergedTexture(renderer.material.mainTexture, true);
                if (texture != null) graphic.genericRenderers.Where(r => r.name == "Body" || r.name == "RightTurbine").Do(r => r.SetMainTexture(texture));
            }
        }

        public static void SetBloonAppearance(Bloon bloon)
        {
            var graphic = bloon?.display?.node?.graphic;
            if (graphic == null) return;
            if (GetBloonByName(bloon.bloonModel.name) is FusionBloonModel fusion) SetBloonAppearance(fusion, graphic);
        }

        public static void OnInGameUpdate(InGame inGame)
        {
            List<BloonToSimulation> bloonSims;
            try { bloonSims = inGame.bridge.GetAllBloons().ToList(); } catch { return; }
            foreach (var bloonSim in bloonSims) { SetBloonAppearance(bloonSim.GetBloon()); }
        }

    }
}
