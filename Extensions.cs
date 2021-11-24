using Assets.Scripts.Simulation.Bloons;
using BTD_Mod_Helper.Extensions;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Combloonation
{
    public static class Extensions
    {
        public static System.Collections.Generic.IEnumerable<System.Tuple<int, int>> GetEnumerator(this Texture2D texture)
        {
            for (int x = 0; x < texture.width; x++ )
            {
                for (int y = 0; y < texture.height; y++ )
                {
                    yield return new System.Tuple<int,int>(x,y);
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
            texture2.ReadPixels(new Rect(rect.x,texture.height - rect.height - rect.y, rect.width, rect.height),0,0);
            texture2.Apply();
            RenderTexture.active = null;
            return texture2;
        }

        public static Texture2D Duplicate(this Texture texture, System.Func<int, int, Color, Color> func, Rect? proj = null)
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

        public static Texture2D GenerateTexture(this Bloon bloon, Texture oldTexture, Rect? proj = null)
        {
            var model = bloon.bloonModel;
            var exists = Labloontory.computedTextures.TryGetValue(model, out var texture);
            if (exists) return texture;
            var color = model.GetBaseColor();
            if (color is Color tint)
            {
                var path = $"{Main.folderPath}/{model.id}.png";
                if (!File.Exists(path))
                {
                    texture = oldTexture.TintMask(tint, proj);
                    texture.SaveToPNGAlt(path);
                }
                texture.LoadFromFile(path);
                Labloontory.computedTextures[model] = texture;
                return texture;
            }
            return null;
        }

        public static void SaveToPNGAlt(this Texture2D texture, string filePath)
        {
            byte[] bytes = ImageConversion.EncodeToPNG(texture).ToArray();
            using (var file = new FileStream(filePath, FileMode.Create))
            {
                file.Write(bytes, 0, bytes.Length);
            }
        }

    }
}
