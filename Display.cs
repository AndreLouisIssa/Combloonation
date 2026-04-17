using System.Collections.Generic;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.Display;
using Il2CppAssets.Scripts.Models.Bloons;
using System;
using UnityEngine;
using static Combloonation.Labloontory;
using static Combloonation.Main;
using static Combloonation.Helpers;
using static Combloonation.RegionScalarMap;
using HarmonyLib;
using UnityEngine.UI;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.BloonMenu;

namespace Combloonation
{
    public static class Display
    {
        private static Color initColor = new(0.929f, 0.059f, 0.059f, 1);
        private static bool patchingIcons = false;
        private static bool patchedIcons = false;
        private static readonly List<Image> patchedImages = [];
        private static Vector2 sizeDelta = default;
        private static Vector3 localScale = default;

        private static readonly Func<Renderer, bool> mainRenderer = r => r.name == "Body" || r.name.Contains("Base") || r.name == "RightTurbine";
        private static readonly Dictionary<string, Texture2D> computedTextures = [];
        private static readonly Dictionary<string, Texture2D> computedIcons = [];
        private static List<string>? bloonMenuFusions = null;
        private static string bloonMenuProperties = "";

        private static readonly IOverlay emptyColor = new DelegateOverlay((c, x, y) => c);
        private static readonly IOverlay invertColor = new DelegateOverlay((c, x, y) => { var t = (float)Math.Round(1 - c.grayscale); return new Color(t, t, t, c.a); });
        private static readonly IOverlay boundaryColor = new DelegateOverlay((c, x, y) => c.RGBMultiplied(0.5f));
        private static readonly IOverlay fortifiedColorA = new ColorOverlay(HexColor("cd5d10"));
        private static readonly IOverlay fortifiedColorB = new ColorOverlay(HexColor("cecece"));
        private static readonly Tuple<List<IOverlay>, List<float>> fortifiedColors = new(
            [emptyColor, fortifiedColorB, fortifiedColorA, fortifiedColorB, emptyColor, fortifiedColorB, fortifiedColorA, fortifiedColorB, emptyColor],
            [30f, 2f, 8f, 2f, 30f, 2f, 8f, 2f, 30f]);

        private static readonly Dictionary<string, IOverlay> baseColors = new()
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
        private static readonly Dictionary<string, IOverlay> missingColors = [];

        public interface IOverlay
        {
            Color Pixel(Color c, float x, float y);
        }

        public class DelegateOverlay(Func<Color, float, float, Color> func) : IOverlay
        {
            public Func<Color, float, float, Color> func = func;

            public Color Pixel(Color c, float x, float y)
            {
                return func(c, x, y);
            }
        }

        public class PipeOverlay(IOverlay a, IOverlay b) : IOverlay
        {
            public IOverlay a = a;
            public IOverlay b = b;

            public Color Pixel(Color c, float x, float y)
            {
                var _c = a.Pixel(c, x, y);
                return b.Pixel(_c, x, y);
            }
        }

        public class RegionOverlay : IOverlay
        {
            public readonly List<IOverlay> cs;
            public List<float> ps;
            public RegionScalarMap map;

            public RegionOverlay(List<IOverlay> cs, List<float> ws, RegionScalarMap map)
            {
                if (cs == null) throw new ArgumentNullException(nameof(cs));
                if (cs.Count != ws.Count) throw new ArgumentException("Weights list must be the same length");
                this.cs = cs; ps = WeightsToPivots(ws); this.map = map;
            }

            public Color Pixel(Color c, float x, float y)
            {
                #pragma warning disable CS8602 // Dereference of a possibly null reference.
                #pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                return cs.SplitRange(ps, null, map, x, y).Pixel(c, x, y);
                #pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                #pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }
        public class CheckeredOverlay(List<IOverlay> cs, float sx, float sy) : IOverlay
        {
            public List<IOverlay> cs = cs;
            readonly float sx = sx;
            readonly float sy = sy;

            public Color Pixel(Color c, float x, float y)
            {
                var n = cs.Count;
                var s = Math.Floor(n * x / sx) + Math.Floor(n * y / sy);
                var m = (int)(s - n * Math.Floor(s / n));
                return cs[m].Pixel(c, x, y);
            }
        }

        public class TintOverlay(IOverlay c) : IOverlay
        {
            public float t = 0.8f;
            public Func<float, float, float>? tf;
            public IOverlay c = c;

            public TintOverlay(IOverlay c, float t) : this(c) { this.t = t; }
            public TintOverlay(IOverlay c, Func<float, float, float> tf) : this(c) { this.tf = tf; }

            public Color Pixel(Color mc, float x, float y)
            {
                var tp = (tf != null) ? tf(x, y) : t;
                var tc = c.Pixel(mc, x, y);
                return Color.Lerp(mc, new Color(tc.r, tc.g, tc.b, mc.a), tp);
            }
        }

        public class ColorOverlay(Color c) : IOverlay
        {

            public Color c = c;

            public Color Pixel(Color c, float x, float y)
            {
                return new Color(this.c.r, this.c.g, this.c.b, c.a);
            }
        }

        public class BoundOverlay(IOverlay ci,  IOverlay co) : IOverlay
        {

            public IOverlay ci = ci;
            public IOverlay co = co;
            public float b = 1f;
            public Func<float, float, bool>? bf;

            public BoundOverlay(IOverlay ci, IOverlay co, float b) : this(ci, co) { this.b = b; }
            public BoundOverlay(IOverlay ci, IOverlay co, Func<float, float, bool> bf) : this(ci, co) { this.bf = bf; }

            public Color Pixel(Color c, float x, float y)
            {
                bool ins;
                if (bf is null) ins = x * x + y * y > b * b;
                else ins = bf(x, y);
                if (ins) return co.Pixel(c, x, y);
                return ci.Pixel(c, x, y);
            }
        }

        public static Color HexColor(string hex)
        {
            #pragma warning disable IDE0057 // Use range operator
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
            #pragma warning restore IDE0057 // Use range operator
        }

        public static Color Average(this Color a, Color b)
        {
            return Color.Lerp(a, b, (1 + b.a - a.a) / 2);
        }

        public static List<IOverlay> GetColors(this BloonModel bloon, Rect bound)
        {
            IEnumerable<string> ids;
            var fusion = bloon.GetFusion();
            if (fusion != null)
                ids = fusion.fusands.Select(f => f.baseId);
            else ids = [bloon.baseId];
            var cols = new List<IOverlay> { };
            foreach (var id in ids)
            {
                var got = baseColors.TryGetValue(id, out var col);
                if (got && col != null) cols.Add(col);
                else cols.Add(GetMissingColor(id, bound));
            }
            return cols;
        }

        private static IOverlay GetMissingColor(string id, Rect b)
        {
            var got = missingColors.TryGetValue(id, out var col);
            if (!got || col == null) col = missingColors[id] = new ColorOverlay(random.NextColor());
            var r = (float)Math.Min(b.width, b.height) / 8;
            return new CheckeredOverlay([invertColor, col], r, r);
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
            proj ??= new Rect(0, 0, texture.width, texture.height);
            var rect = (Rect)proj;
            texture.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height);
            rt.filterMode = FilterMode.Point;
            RenderTexture.active = rt;
            Graphics.Blit(texture, rt);
            Texture2D texture2 = new((int)rect.width, (int)rect.height);
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
            proj ??= new Rect(0, 0, texture.width, texture.height);
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
            if (proj is Rect rect)
            {
                w = rect.width; h = rect.height;
                x = rect.x; y = rect.y;
            }
            else
            {
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

        public static Texture2D NewMergedTexture(this Fusion fusion, Texture texture, bool fromMesh, Rect? proj = null)
        {
            if (fusion is null) throw new ArgumentNullException(nameof(fusion));
            var bloon = fusion.bloon;

            var bound = GetRegionRect(texture, proj);
            var cols = GetColors(fusion.bloon, bound);
            var r = Math.Min(bound.width, bound.height) / 2;
            var dx = 0f; var dy = 0f;
            var fbase = fusion.fusands.First();
            var tcols = cols.Skip(1).ToList();
            IOverlay? dcol = null; IOverlay ddcol = emptyColor;
            if (cols.Count > 1)
            {
                var ws = fusion.fusands.Skip(1).Select(b => b.danger).ToList();
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
                if (dcol is null)
                {
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
                col = new PipeOverlay(col, new DelegateOverlay((c, x, y) =>
                {
                    x *= cmx; y *= cmy;
                    var n1 = Mathf.PerlinNoise(18.05f + x / 31f, 67f + y / 17f);
                    var n2 = Mathf.PerlinNoise(184f + x / 20f, 627f + y / 8f);
                    return c.RGBMultiplied((float)Math.Ceiling(4 * n1 + 2 * n2) / 6 + 0.3f);
                }));
            }
            if (!fbase.isFortified && bloon.isFortified)
            {
                col = new PipeOverlay(col, new RegionOverlay(fortifiedColors.Item1, fortifiedColors.Item2,
                    Regions.vertical(bound.x, bound.x + bound.width, bound.y, bound.y + bound.height)));
            }
            r_iob = r * 0.6f; r_iib = 0.85f * r_iob; r_oob = r_iob * 1.15f;
            float tf(float x, float y) => (float)TERF(curve(x / r_oob, y / r_oob), 1f, -1f);
            dcol ??= emptyColor;
            var bcol = new BoundOverlay(dcol, ddcol, (x, y) => curve(x / r_iib, y / r_iib) >= 0);
            var bbcol = new BoundOverlay(bcol, dcol, (x, y) => curve(x / r_iob, y / r_iob) >= 0);
            var tcol = new TintOverlay(bbcol, tf);
            var bbbcol = new BoundOverlay(tcol, emptyColor, (x, y) => curve(x / r_oob, y / r_oob) >= 0);
            col = new PipeOverlay(col, bbbcol);

            return texture.Duplicate((x, y, c) => col.Pixel(c, x + (int)(dx + bound.x), y + (int)(dy + bound.y)), proj);
        }

        public static Texture2D? GetMergedTexture(this Fusion fusion, Texture oldTexture, Dictionary<string, Texture2D> computed, bool fromMesh, Rect? proj = null)
        {
            if (fusion is null) throw new ArgumentNullException(nameof(fusion));
            var bloon = fusion.bloon;

            var exists = computed.TryGetValue(bloon.name, out var texture);
            if (exists) return texture;

            var postfix = fromMesh ? "texture" : "icon";
            // TODO: restore option to get texture from file?

            if (oldTexture is null) return null;
            if (oldTexture.isReadable) return null;

            texture = fusion.NewMergedTexture(oldTexture, fromMesh, proj);
            if (texture is null) return null;
            computed[bloon.name] = texture;

            if (FolderPath != null) texture.SaveToPNG($"{FolderPath}/{DebugString(bloon.name)}.{postfix}.png");

            return texture;
        }

        public static void SetBloonAppearance(this Fusion fusion, UnityDisplayNode graphic)
        {
            if (fusion is null) throw new ArgumentNullException(nameof(fusion));
            var bloon = fusion.bloon;

            var sprite = graphic.sprite;
            if (sprite is null)
            {
                var renderer = graphic.genericRenderers.First(mainRenderer);
                var texture = fusion.GetMergedTexture(renderer.material.mainTexture, computedTextures, true);
                if (texture is null) return;
                graphic.genericRenderers.Where(mainRenderer).Do(r => r.SetMainTexture(texture));
            }
            else
            {
                var ssprite = sprite.sprite;
                var correction = new Vector2(0,3910);
                Rect rect = new(ssprite.textureRect.position - correction, ssprite.textureRect.size);

                var texture = fusion.GetMergedTexture(ssprite.texture, computedIcons, false, rect);
                if (texture is null) return;
                sprite.sprite = texture.CreateSpriteFromTexture(sprite.sprite.pixelsPerUnit);
            }

        }

        public static void SetBloonAppearance(this Fusion fusion, Image icon)
        {
            if (fusion is null) throw new ArgumentNullException(nameof(fusion));
            var bloon = fusion.bloon;

            var sprite = icon.sprite;
            if (sprite.texture.isReadable) return;

            if (bloonMenuFusions is null) throw new NullReferenceException($"{bloonMenuFusions} is null!"); // TODO: can we just guard this null check?

            if (!patchedIcons && !computedIcons.ContainsKey(bloon.name) && sprite.GetCenterColor().IsSimilar(initColor)) return;

            var texture = fusion.GetMergedTexture(sprite.texture, computedIcons, false, sprite.textureRect);
            if (texture == null) return;

            icon.SetSprite(texture.CreateSpriteFromTexture(sprite.pixelsPerUnit));

            var rt = icon.rectTransform;
            sizeDelta = rt.sizeDelta; rt.sizeDelta = new Vector2(2, 2);
            localScale = rt.localScale; rt.localScale = new Vector3(texture.width / 110f, texture.height / 110f);
            bloonMenuFusions.Remove(bloon.name);
            patchedImages.Add(icon);
        }

        public static void SetBloonAppearance(Bloon bloon)
        {
            var graphic = bloon.Display?.node?.graphic;
            if (graphic is null) return;
            var fusion = FusionFromNameSafe(bloon.bloonModel.name);
            if (fusion != null) SetBloonAppearance(fusion, graphic);
        }

        public static void SetBloonAppearance(SpawnBloonButton button)
        {
            if (patchingIcons)
            {
                var fusion = FusionFromNameSafe(button.model.name);
                if (fusion != null)
                {
                    fusion.SetBloonAppearance(button.Button.image);
                    if (!patchedIcons && (bloonMenuFusions != null && bloonMenuFusions.Count == 0))
                    {
                        patchedIcons = true;
                        patchingIcons = false;
                        Log("Finished setting icons!");
                    }
                }
            }
        }

        public static void InGameUpdate(InGame inGame)
        {
            if (inGame.bridge is null) return;
            List<BloonToSimulation> bloonSims;
            try { bloonSims = inGame.bridge.GetAllBloons().ToList(); } catch { return; }
            foreach (var bloonSim in bloonSims) { SetBloonAppearance(bloonSim.GetBloon()); }
        }

        public static void BloonMenuSortBloons(BloonMenu menu)
        {
            try { foreach (var icon in patchedImages) {
                var rt = icon.rectTransform;
                rt.sizeDelta = sizeDelta;
                rt.localScale = localScale;
            } } catch { patchedIcons = false; }
            patchedImages.Clear();
            menu.ClearButtons();
            bloonMenuProperties = PropertyString(Property.all.Where(p => p.menu(menu)));
            IEnumerable<BloonModel> bloons = GetGameModel().bloons.OrderBy(b => b.danger);
            if (patchedIcons) bloons = bloons.Where(b => PropertyString(GetProperties(b)) == bloonMenuProperties);
            menu.CreateBloonButtons(bloons.ToIl2CppList());
            if (!patchingIcons && !patchedIcons) {
                Log("Setting icons...");
                bloonMenuFusions = [.. bloons.Select(b => b.name).Where(n => FusionFromNameSafe(n) != null)];
                patchingIcons = true;
            }
        }

        public static bool RepatchIcons()
        {
            return patchingIcons = patchedIcons;
        }
    }
}
