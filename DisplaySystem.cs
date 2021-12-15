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
using static Combloonation.Main;
using static Combloonation.Helpers;
using static Combloonation.RegionScalarMap;
using MelonLoader;
using HarmonyLib;
using UnityEngine.UI;
using Assets.Scripts.Unity.UI_New.InGame.BloonMenu;
using Assets.Scripts.Utils;

namespace Combloonation
{
    public static class DisplaySystem
    {
        public static Color initColor = new Color(0.929f, 0.059f, 0.059f, 1);
        public static bool tryPatchingIcons = true;

        public static Func<Renderer,bool> mainRenderer = r => r.name == "Body" || r.name.Contains("Base") || r.name == "RightTurbine";
        public static Dictionary<string, Texture2D> computedTextures = new Dictionary<string, Texture2D>();
        public static Dictionary<string, Texture2D> computedIcons = new Dictionary<string, Texture2D>();

        public static IOverlay invisColor = new DelegateOverlay((c,x,y) => new Color(0,0,0,0));
        public static IOverlay emptyColor = new DelegateOverlay((c,x,y) => c);
        public static IOverlay invertColor = new DelegateOverlay((c,x,y) => {var t = (float)Math.Round(1 - c.grayscale); return new Color(t, t, t, c.a);});
        public static IOverlay boundaryColor = new DelegateOverlay((c,x,y)=> c.RGBMultiplied(0.5f));
        public static IOverlay fortifiedColorA = new ColorOverlay(HexColor("cd5d10"));
        public static IOverlay fortifiedColorB = new ColorOverlay(HexColor("cecece"));
        public static Tuple<List<IOverlay>, List<float>> fortifiedColors = new Tuple<List<IOverlay>, List<float>>(
            new List<IOverlay> { emptyColor, fortifiedColorB, fortifiedColorA, fortifiedColorB, emptyColor, fortifiedColorB, fortifiedColorA, fortifiedColorB, emptyColor },
            new List<float> { 30f, 2f, 8f, 2f, 30f, 2f, 8f, 2f, 30f });

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
        public static Dictionary<string, IOverlay> missingColors = new Dictionary<string, IOverlay>();

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
        public class CheckeredOverlay : IOverlay
        {
            public List<IOverlay> cs;
            float sx;
            float sy;

            public CheckeredOverlay(List<IOverlay> cs, float sx, float sy)
            {
                this.cs = cs; this.sx = sx; this.sy = sy;
            }

            public Color Pixel(Color c, float x, float y)
            {
                var n = cs.Count;
                var s = Math.Floor(n * x / sx) + Math.Floor(n * y / sy);
                var m = (int)(s - n*Math.Floor(s/n));
                return cs[m].Pixel(c, x, y);
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

        public static List<IOverlay> GetColors(this BloonModel bloon, Rect bound)
        {
            var ids = BaseBloonNamesFromName(bloon.name);
            var cols = new List<IOverlay> { };
            foreach (var id in ids)
            {
                var got = baseColors.TryGetValue(id, out var col);
                if (got) cols.Add(col);
                else cols.Add(GetMissingColor(id, bound));
            }
            return cols;
        }

        private static IOverlay GetMissingColor(string id, Rect b)
        {
            var got = missingColors.TryGetValue(id, out var col);
            if (!got) col = missingColors[id] = new ColorOverlay(random.NextColor());
            var r = (float)Math.Min(b.width,b.height)/8;
            return new CheckeredOverlay(new List<IOverlay>{ invertColor, col }, r, r);
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
            var bound = GetRegionRect(texture, proj);
            var cols = GetColors(bloon, bound);
            var r = Math.Min(bound.width, bound.height) / 2;
            var dx = 0f; var dy = 0f;
            var fbase = bloon.fusands.First();
            var tcols = cols.Skip(1).ToList();
            IOverlay dcol = null; IOverlay ddcol = emptyColor;
            if (cols.Count > 1)
            {
                var ws = bloon.fusands.Skip(1).Select(b => b.danger).ToList();
                var map = Regions.spiral(1.3f, 0.6f)(bound.x, bound.x + bound.width, bound.y, bound.y + bound.height);
                r *= ws[0] / fbase.danger;
                dcol = new RegionOverlay(tcols, ws, map);
            }
            if (fromMesh)
            {
                dx = bound.width * 0.165f;
                if (bloon.isBoss) dx = -dx;
                dy = bound.height * 0.1f;
                r *= 0.5f;
            }
            else if (!fbase.isGrow) dy = -bound.height * 0.05f;
            float r_iob, r_iib, r_oob;
            Func<float, float, float> curve;
            if (!fbase.isGrow && bloon.isGrow)
            {
                curve = (x, y) => (float)HeartCurve(x, y);
                r *= 0.9f;
                if (dcol == null) {
                    ddcol = boundaryColor;
                    r *= 0.75f;
                }
            }
            else
            {
                curve = (x, y) => (float)CircleCurve(x, y);
                r *= 1.25f;
            }
            var col = emptyColor;
            if (!fbase.isCamo && bloon.isCamo)
            {
                var cmx = 66f / bound.width; var cmy = 84f / bound.height;
                col = new PipeOverlay(col,new DelegateOverlay((c,x,y) => { 
                    x *= cmx; y *= cmy;
                    var n1 = Mathf.PerlinNoise(18.05f + x / 31f, 67f + y / 17f);
                    var n2 = Mathf.PerlinNoise(184f + x / 20f,627f + y / 8f);
                    return c.RGBMultiplied((float)Math.Ceiling(4*n1 + 2*n2)/6 + 0.3f);
                }));
            }
            if (!fbase.isFortified && bloon.isFortified)
            {
                col = new PipeOverlay(col,new RegionOverlay(fortifiedColors.Item1, fortifiedColors.Item2,
                    Regions.vertical(bound.x,bound.x + bound.width, bound.y, bound.y + bound.height)));
            }
            r_iob = r*0.6f; r_iib = 0.85f*r_iob; r_oob = r_iob * 1.15f;
            Func<float, float, float> tf = (x, y) => (float)TERF(curve(x / r_oob, y / r_oob), 1f, -1f);
            if (dcol == null) dcol = emptyColor;
            var bcol = new BoundOverlay(dcol, ddcol, (x, y) => curve(x / r_iib, y / r_iib) >= 0);
            var bbcol = new BoundOverlay(bcol, dcol, (x, y) => curve(x / r_iob, y / r_iob) >= 0);
            var tcol = new TintOverlay(bbcol, tf);
            var bbbcol = new BoundOverlay(tcol, emptyColor, (x, y) => curve(x / r_oob, y / r_oob) >= 0);
            col = new PipeOverlay(col, bbbcol);
            return texture.Duplicate((x, y, c) => col.Pixel(c, x + (int)(dx + bound.x), y + (int)(dy + bound.y)), proj);
        }

        public static Texture2D GetMergedTexture(this FusionBloonModel bloon, Texture oldTexture, Dictionary<string, Texture2D> computed, bool fromMesh, string postfix, Rect? proj = null)
        {
            if (bloon == null) throw new ArgumentNullException(nameof(bloon));
            if (oldTexture == null) return computed[bloon.name] = null;
            if (oldTexture.isReadable) return null;
            var exists = computed.TryGetValue(bloon.name, out var texture);
            if (exists) return texture;
            computed[bloon.name] = texture = bloon.NewMergedTexture(oldTexture, fromMesh, proj);
            if (texture != null) {
                texture.SaveToPNG($"{folderPath}/{DebugString(bloon.name)}.{postfix}.png");
                if (computed == computedIcons) bloon.SetHelpfulAdditionsBloon();
            }
            return texture;
        }

        public static void SetBloonAppearance(this FusionBloonModel bloon, UnityDisplayNode graphic)
        {
            var sprite = graphic.sprite;
            if (sprite != null)
            {
                var texture = bloon.GetMergedTexture(sprite.sprite.texture, computedIcons, false, "icon", sprite.sprite.textureRect);
                if (texture != null)
                {
                    sprite.sprite = texture.CreateSpriteFromTexture(sprite.sprite.pixelsPerUnit);
                }
            }
            else
            {
                var renderer = graphic.genericRenderers.First(mainRenderer);
                var texture = bloon.GetMergedTexture(renderer.material.mainTexture, computedTextures, true, "texture");
                if (texture != null) graphic.genericRenderers.Where(mainRenderer).Do(r => r.SetMainTexture(texture));
            }
        }

        public static void SetBloonAppearance(this FusionBloonModel bloon, Image icon)
        {
            var sprite = icon.sprite;
            if (sprite.texture.isReadable || sprite.GetCenterColor().IsSimilar(initColor)) return;
            var texture = bloon.GetMergedTexture(sprite.texture, computedIcons, false, "icon", sprite.textureRect);
            if (texture != null) {
                icon.SetSprite(texture.CreateSpriteFromTexture(sprite.pixelsPerUnit));
                icon.rectTransform.sizeDelta = new Vector2(2,2);
                icon.rectTransform.localScale = new Vector3(texture.width / 110f, texture.height / 110f);
                //MelonLogger.Msg("Set icon of " + DebugString(bloon.name));
            } 
        }

        public static void SetHelpfulAdditionsBloon(this FusionBloonModel bloon)
        {
            Func<float,float,float> ms = (x,y) => x*x + y*y;
            var ox = 25; var oy = 50; var or = ox * ox;
            var ix = 20; var ir = ix * ix;
            var name = bloon.name;
            var icon = computedIcons[name];
            var bound = new Rect(0, 0, ox, oy);
            var cols = GetColors(bloon, bound); cols.Reverse();
            var map = Regions.vertical(0, ox, 0, oy);
            var ws = bloon.fusands.Select(b => 1f).ToList();
            var bcol = boundaryColor;
            var mcol = new RegionOverlay(cols, ws, map);
            var scol = new PipeOverlay(mcol,new BoundOverlay(emptyColor, bcol, (x,y) => Math.Abs(y - ox) > ix));
            var span = new Texture2D(ox, oy).Duplicate((x, y, c) => scol.Pixel(c, x, y));
            var ecol = new BoundOverlay(new PipeOverlay(mcol, new BoundOverlay(emptyColor, bcol, (x,y) => ms(x-ox,y-ox) > ir)), invisColor, (x,y) => ms(x-ox,y-ox) > or);
            var edge = new Texture2D(ox, oy).Duplicate((x, y, c) => ecol.Pixel(c, x, y));
            optional_HelpfulAdditions_AddCustomBloon.Invoke(null, new object[] {
                name, icon, edge, span, new Vector2(icon.width*2, icon.height*2)
            });
        }

        public static void SetBloonAppearance(Bloon bloon)
        {
            var graphic = bloon?.display?.node?.graphic;
            if (graphic == null) return;
            if (BloonFromName(bloon.bloonModel.name) is FusionBloonModel fusion) SetBloonAppearance(fusion, graphic);
        }

        public static void SetBloonAppearance(SpawnBloonButton button)
        {
            if (BloonFromName(button.model.name) is FusionBloonModel bloon)
            {
                bloon.SetBloonAppearance(button.Button.image);
            }
        }

        public static void SetBloonAppearance(InGame inGame)
        {
            if (inGame.bridge == null) return;
            List<BloonToSimulation> bloonSims;
            try { bloonSims = inGame.bridge.GetAllBloons().ToList(); } catch { return; }
            foreach (var bloonSim in bloonSims) { SetBloonAppearance(bloonSim.GetBloon()); }
        }

    }
}
