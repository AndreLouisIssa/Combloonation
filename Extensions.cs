using Assets.Scripts.Simulation.Bloons;
using BTD_Mod_Helper.Extensions;
using System.IO;
using UnityEngine;

namespace Combloonation
{
    public static class Extensions
    {
        public static System.Collections.Generic.IEnumerable<System.Tuple<int, int>> GetEnumerator(this Texture2D texture)
        {
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    yield return new System.Tuple<int, int>(x, y);
                }
            }
        }

        public static Texture2D Duplicate(this Texture texture)
        {
            texture.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height);
            rt.filterMode = FilterMode.Point;
            RenderTexture.active = rt;
            Graphics.Blit(texture, rt);
            Texture2D texture2 = new Texture2D(texture.width, texture.height);
            texture2.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture2.Apply();
            RenderTexture.active = null;
            return texture2;
        }

        public static Texture2D Duplicate(this Texture texture, System.Func<int, int, Color, Color> func)
        {
            var t = texture.Duplicate();
            foreach (var xy in t.GetEnumerator())
            {
                var x = xy.Item1; var y = xy.Item2;
                t.SetPixel(x, y, func(x, y, t.GetPixel(x, y)));
            }
            return t;
        }

        public static Texture2D TintMask(this Texture texture, Color tint)
        {
            return texture.Duplicate((x, y, c) => Labloontory.TintMask(tint, c));
        }

        public static Texture2D GenerateTexture(this Bloon bloon, Texture oldTexture)
        {
            var model = bloon.bloonModel;
            var exists = Labloontory.computedTextures.TryGetValue(model, out var texture);
            if (exists) return texture;
            var color = model.GetBaseColor();
            if (color is Color tint)
            {
                texture = oldTexture.TintMask(tint);
                var path = $"{Main.folderPath}/{model.id}.png";
                if (File.Exists(path))
                {
                    texture.LoadFromFile(path);
                }
                else
                {
                    texture.SaveToPNG(path);
                }
                Labloontory.computedTextures[model] = texture;
                return texture;
            }
            return null;
        }
    }
}
