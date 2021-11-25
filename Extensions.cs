using Assets.Scripts.Simulation.Bloons;
using BTD_Mod_Helper.Extensions;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Combloonation
{
    public static class Extensions
    {
        public static IEnumerable<System.Tuple<int, int>> GetEnumerator(this Texture2D texture)
        {
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    yield return new System.Tuple<int, int>(x, y);
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
            return t;
        }

        public static Texture2D TintMask(this Texture texture, Color tint, Rect? proj = null)
        {
            return texture.Duplicate((x, y, c) => Labloontory.TintMask(tint, c), proj);
        }

        public static int SplitRange(int n, int lo, int hi, int x)
        {
            int d = x - lo;
            int m = hi - lo;
            int mn = m / n;
            int i = d / mn;
            return Math.Min(Math.Max(0,i),n - 1);
        }

        public static T SplitRange<T>(this List<T> list, int lo, int hi, int x)
        {
            return list[SplitRange(list.Count, lo, hi, x)];
        }

        public static Texture2D TintMask(this Texture texture, List<Color> tints, Rect? proj = null)
        {
            if (tints == null) throw new ArgumentNullException(nameof(tints));
            int h = proj is Rect rect ? (int)rect.height : texture.height;
            return texture.Duplicate((x, y, c) => Labloontory.TintMask(tints.SplitRange(0, texture.height, y), c), proj);
        }

        public static Texture2D GenerateTexture(this Bloon bloon, Texture oldTexture, Rect? proj = null)
        {
            if (bloon == null) throw new ArgumentNullException(nameof(bloon));
            var model = bloon.bloonModel;
            var exists = Labloontory.computedTextures.TryGetValue(model, out var texture);
            if (exists) return texture;
            var tints = model.GetBaseColors();
            if (tints.Count == 0) return Labloontory.computedTextures[model] = null;
            texture = oldTexture.TintMask(tints, proj);
            texture.Reload($"{Main.folderPath}/{model.id}.png");
            Labloontory.computedTextures[model] = texture;
            return texture;
        }

        public static void Reload(this Texture2D texture, string path = null)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            byte[] bytes = ImageConversion.EncodeToPNG(texture).ToArray();
            if (path != null) using (var file = new FileStream(path, FileMode.Create)) file.Write(bytes, 0, bytes.Length);
            ImageConversion.LoadImage(texture, bytes);
        }

    }
}
