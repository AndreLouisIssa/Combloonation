using Assets.Scripts.Models;
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
            r = r ?? new Random();
            var pivots = Enumerable.Repeat(0, parts - 1).Select(z => r.Next(1, size)).Append(0).Append(size).OrderBy(n => n);
            var sizes = new List<int> { };
            var s = pivots.First();
            foreach (var pivot in pivots.Skip(1))
            {
                sizes.Add(pivot - s);
                s = pivot;
            }
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

        public static DirectableModel ToDirectable<M>(this M m) where M : Model
        {
            return new DirectableModel((dynamic)m);
        }

        public static IEnumerable<DirectableModel> ToDirectable<M>(this IEnumerable<M> ms) where M : Model
        {
            return ms.Select(m => m.ToDirectable());
        }

        public static M ToModel<M>(this DirectableModel m) where M : Model
        {
            return m.Cast<M>();
        }

        public static IEnumerable<M> ToModel<M>(this IEnumerable<DirectableModel> ms) where M : Model
        {
            return ms.Select(m => m.Cast<M>());
        }
    }
}
