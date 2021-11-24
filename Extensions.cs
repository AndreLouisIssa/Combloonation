using UnityEngine;
using Object = Il2CppSystem.Object;

namespace Combloonation
{
    public static class Extensions
    {
        public static System.Reflection.FieldInfo GetFieldInfo(this object obj, string name)
        {
            return obj.GetType().GetField($"<{name}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        }

        public static void SetFieldValue(this object obj, string name, object value)
        {
            obj.GetFieldInfo(name).SetValue(obj, value);
        }

        public static object GetFieldValue(this object obj, string name)
        {
            return obj.GetFieldInfo(name).GetValue(obj);
        }

        public static System.Reflection.PropertyInfo GetPropertyInfo(this object obj, string name)
        {
            return obj.GetType().GetProperty(name, System.Reflection.BindingFlags.Instance);
        }

        public static void SetPropertyValue(this object obj, string name, object value)
        {
            obj.GetPropertyInfo(name).SetValue(obj, value, null);
        }

        public static object GetPropertyValue(this object obj, string name)
        {
            return obj.GetPropertyInfo(name).GetValue(obj);
        }

        public static Il2CppSystem.Reflection.FieldInfo GetFieldInfo(this Object obj, string name)
        {
            return obj.GetIl2CppType().GetField($"<{name}>k__BackingField", Il2CppSystem.Reflection.BindingFlags.Instance);
        }

        public static void SetFieldValue(this Object obj, string name, Object value)
        {
            obj.GetFieldInfo(name).SetValue(obj, value);
        }

        public static Object GetFieldValue(this Object obj, string name)
        {
            return obj.GetFieldInfo(name).GetValue(obj);
        }

        public static Il2CppSystem.Reflection.PropertyInfo GetPropertyInfo(this Object obj, string name)
        {
            return obj.GetIl2CppType().GetProperty(name, Il2CppSystem.Reflection.BindingFlags.Instance);
        }

        public static void SetPropertyValue(this Object obj, string name, Object value)
        {
            var info = obj.GetPropertyInfo(name);
            var index = info.GetIndexParameters();
            info.SetValue(obj, value, null);
        }

        public static Object GetPropertyValue(this Object obj, string name)
        {
            return obj.GetPropertyInfo(name).GetValue(obj);
        }

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

        public static Texture2D Duplicate(this Texture2D texture)
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

        public static Texture2D Duplicate(this Texture2D texture, System.Func<int, int, Color, Color> func)
        {
            var t = texture.Duplicate();
            foreach (var xy in t.GetEnumerator())
            {
                var x = xy.Item1; var y = xy.Item2;
                t.SetPixel(x, y, func(x, y, t.GetPixel(x, y)));
            }
            return t;
        }
    }
}
