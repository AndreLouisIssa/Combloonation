﻿using Assets.Scripts.Models;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;

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
            x = 1.3d*Math.Abs(x);
            y = (y + 0.25d) * 1.25d;
            var z = x * x + y * y - x * (y + 0.75d);
            return Math.Sign(z)*Math.Sqrt(Math.Abs(z)/2) - 1;
        }

        public static double CircleCurve(double x, double y)
        {
            return Math.Sqrt(x*x+y*y) - 1;
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
            return (ERF(((x-s)/(r-s)*4d)-2d)+1d)/2d;
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

        public static SortedList<float, DirectableModel> Sort(this IDirector director, IEnumerable<DirectableModel> ms)
        {
            var list = new SortedList<float, DirectableModel>(ms.Count());
            list.AddValues(ms, m => director.Eval(m));
            return list;
        }

        public static SortedList<float, Model> Sort(this IDirector director, IEnumerable<Model> ms)
        {
            var list = new SortedList<float, Model>(ms.Count());
            list.AddValues<float, Model>(ms, m => director.Eval((dynamic)m));
            return list;
        }

        public static DirectableModel Directable<M>(this M m) where M : Model
        {
            return new DirectableModel((dynamic)m);
        }

        public static IEnumerable<DirectableModel> Directable<M>(this IEnumerable<M> ms) where M : Model
        {
            return ms.Select(m => m.Directable());
        }

        public static IEnumerable<M> Cast<M>(this IEnumerable<DirectableModel> ms) where M : Model
        {
            return ms.Select(m => m.Cast<M>());
        }
    }
}
