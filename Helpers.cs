using Assets.Scripts.Models;
using Assets.Scripts.Models.Rounds;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.UI_New.InGame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = System.Random;
using Bounds = Assets.Scripts.Models.Rounds.FreeplayBloonGroupModel.Bounds;

namespace Combloonation
{
    public static class Helpers
    {
        //https://stackoverflow.com/a/5807166
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list, Random r = null)
        {
            r = r ?? new Random();
            var shuffledList = list.
                Select(x => new { Number = r.Next(), Item = x }).
                OrderBy(x => x.Number).
                Select(x => x.Item);
            return shuffledList;
        }

        public static double HeartCurve(double x, double y)
        {
            x = 1.3d * Math.Abs(x);
            y = (y + 0.25d) * 1.25d;
            var z = x * x + y * y - x * (y + 0.75d);
            return Math.Sign(z) * Math.Sqrt(Math.Abs(z) / 2) - 1;
        }

        public static double CircleCurve(double x, double y)
        {
            return Math.Sqrt(x * x + y * y) - 1;
        }

        //https://www.johndcook.com/blog/csharp_erf/
        public static double ERF(double x)
        {
            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }

        public static double TERF(double x, double s, double r)
        {
            // transformed error function to interpolate from near 0 to near 1 from s to r
            return (ERF(((x - s) / (r - s) * 4d) - 2d) + 1d) / 2d;
        }

        public static void AddValues<K, V>(this SortedList<K, V> list, IEnumerable<V> values, Func<V, K> selector)
        {
            foreach (var value in values) list.Add(selector(value), value);
        }

        public static void AddKeys<K, V>(this SortedList<K, V> list, IEnumerable<K> keys, Func<K, V> selector)
        {
            foreach (var key in keys) list.Add(key, selector(key));
        }

        public static void AddItems<T>(this List<T> list, IEnumerable<T> items)
        {
            foreach (var item in items) list.Add(item);
        }

        public static int[] Partition(int size, int parts, Random r = null)
        {
            //MelonLogger.Msg(size + "/" + parts);
            r = r ?? new Random();
            var _pivots = new HashSet<int>(parts - 1) { 0, size };
            for (int i = 1; i < parts; i++)
            {
                int _r;
                do _r = r.Next(1, size);
                while (_pivots.Contains(_r));
                _pivots.Add(_r);
            }
            var pivots = _pivots.OrderBy(n => n);
            var sizes = new List<int> { };
            //MelonLogger.Msg(string.Join("->",pivots));
            var s = pivots.First();
            foreach (var pivot in pivots.Skip(1))
            {
                sizes.Add(pivot - s);
                s = pivot;
            }
            //MelonLogger.Msg(string.Join("|",sizes));
            return sizes.ToArray();
        }

        //https://stackoverflow.com/questions/50300125/how-to-find-consecutive-same-values-items-as-a-linq-group
        public static IEnumerable<IEnumerable<T>> GroupWhile<T>(this IEnumerable<T> seq, Func<T, T, bool> condition)
        {
            T prev = seq.First();
            List<T> list = new List<T>() { prev };

            foreach (T item in seq.Skip(1))
            {
                if (condition(prev, item) == false)
                {
                    yield return list;
                    list = new List<T>();
                }
                list.Add(item);
                prev = item;
            }

            yield return list;
        }

        public static int[] ArgUnbounded1DKnapsack(int total, int[] val)
        {
            var n = val.Length;
            int _i = 0; int w = 0;
            var vals = val.Select(i => new Tuple<int, int>(i, _i++)).OrderByDescending(i => i.Item1).ToArray();
            Tuple<int, int>[] best = Enumerable.Repeat(new Tuple<int, int>(0, 0), total + 1).ToArray();
            for (int i = 0; i <= total; i++) for (int j = 0; j < n; j++)
                {
                    if (vals[j].Item1 > i) continue;
                    var v = best[i - vals[j].Item1].Item1 + vals[j].Item1;
                    if (v > best[i].Item1) best[i] = new Tuple<int, int>(v, j + 1);
                    if (v > w) w = v;
                }
            var list = new List<int>();
            int k;
            while (true)
            {
                if (w <= 0) break;
                k = best[w].Item2 - 1;
                if (k < 0) break;
                list.Add(vals[k].Item2);
                w = w - vals[k].Item1;
            }
            return list.ToArray();
        }

        public static Color NextColor(this Random random)
        {
            return Color.HSVToRGB((float)random.NextDouble(), 1f, 1f);
        }

        public static Color GetCenterColor(this Sprite sprite)
        {
            var rect = sprite.textureRect;
            var w = sprite.texture.width / 2; var h = sprite.texture.height / 2;
            var texture = sprite.texture.isReadable ? sprite.texture : sprite.texture.Duplicate(rect);
            return texture.GetPixel(w, h);
        }

        public static bool IsSimilar(this Color color, Color other)
        {
            var dr = color.r - other.r;
            var dg = color.g - other.g;
            var db = color.b - other.b;
            var da = color.a - other.a;
            return dr * dr + dg * dg + db * db + da * da < 0.001;
        }

        public static List<List<T>> Power<T>(this List<T> list)
        {
            List<List<T>> power(List<List<T>> p, List<T> s)
            {
                if (s.Count == 0) return p;
                if (s.Count > 1) p = power(p, s.Skip(1).ToList());
                var n = s.First();
                return p.Concat(p.Select(e => e.Append(n).ToList())).ToList();
            }
            return power(new List<List<T>> { new List<T> { } }, list);
        }

        public static Texture2D LoadTexture(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(0, 0) { wrapMode = TextureWrapMode.Clamp };
            ImageConversion.LoadImage(tex, data);
            return tex;
        }

        public static IEnumerable<T> Iterate<T>(this T t, Func<T, T> f)
        {
            while (true) { yield return t; t = f(t); }
        }

        public static T Apply<T>(this T t, params Action<T>[] fs)
        {
            foreach (var f in fs) f(t); return t;
        }

        public static T Apply<T>(this T t, params Func<T, T>[] fs)
        {
            foreach (var f in fs) t = f(t); return t;
        }

        public static GameModel GetGameModel()
        {
            var model = InGame.instance?.bridge?.Model;
            if (model is null) model = Game.instance.model;
            return model;
        }

        public static Bounds NewBounds(int lowerBounds, int upperBounds)
        {
            var bound = new Bounds();
            bound.lowerBounds = lowerBounds;
            bound.upperBounds = upperBounds;
            return bound;
        }

        public class RoundBloonGroupModel : FreeplayBloonGroupModel
        {
            public RoundBloonGroupModel(BloonGroupModel group, int? round) 
                : base("", 0, round is null ? new Bounds[] { } : new Bounds[] { NewBounds((int)round, (int)round) }, group) { }
        }
    }
}
