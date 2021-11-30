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

        public static int[] Partition(int size, int parts, Random r = null)
        {
            r = r ?? new Random();
            var pivots = new HashSet<int>(Enumerable.Repeat(0, parts - 1).Select(z => r.Next(1, size)).Append(0).Append(size));
            var sizes = new List<int> { };
            size = pivots.First();
            foreach (var pivot in pivots.Skip(1))
            {
                sizes.Add(pivot - size);
                size = pivot;
            }
            return sizes.ToArray();
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
    }
}
