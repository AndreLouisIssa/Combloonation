using MelonLoader;
using System;
using System.Collections.Generic;
using Math = Assets.Scripts.Simulation.SMath.Math;

namespace Combloonation
{
    public class RegionScalarMap
    {
        public static class Maps
        {
            public static Func<float, float, float> vertical = (x, y) => y;
            public static Func<float, float, float> horizontal = (x, y) => x;
            public static Func<float, float, float> annular = (x, y) => Magnitude(x, y);
            public static Func<float, float, float> radial = (x, y) => PositiveArgument(x, y);
            public static Func<float, Func<float, float, float>> linear = (a) => (x, y) => Math.Cos(a) * y - Math.Sin(a) * x;
            public static Func<float, float, Func<float, float, float>> spiral = (r, a) => (x, y) =>
            {
                return Modulo(Math.Atan2(x, y) + a * (float)System.Math.Log(Magnitude(x, y), r), Math.TWOPI);
            };
        }

        public static class Regions
        {
            public static Func<float, float, float, float, RegionScalarMap> vertical = (xlo, xhi, ylo, yhi) =>
            {
                return new RegionScalarMap(xlo, xhi, ylo, yhi, ylo, yhi, Maps.vertical);
            };

            public static Func<float, float, float, float, RegionScalarMap> horizontal = (xlo, xhi, ylo, yhi) =>
            {
                return new RegionScalarMap(xlo, xhi, ylo, yhi, xlo, xhi, Maps.horizontal);
            };

            public static Func<float, float, float, float, RegionScalarMap> annular = (xlo, xhi, ylo, yhi) =>
            {
                float x; float y;
                if (xlo < 0 && 0 < xhi) x = 0; else x = MinBound(xlo, xhi);
                if (ylo < 0 && 0 < yhi) y = 0; else y = MinBound(ylo, yhi);
                var zlo = Magnitude(x, y);
                var zhi = Magnitude(MaxBound(xlo, xhi), MaxBound(ylo, yhi));
                return new RegionScalarMap(xlo, xhi, ylo, yhi, zlo, zhi, Maps.annular);
            };

            public static Func<float, float, float, float, RegionScalarMap> radial = (xlo, xhi, ylo, yhi) =>
            {
                return new RegionScalarMap(xlo, xhi, ylo, yhi, 0, Math.TWOPI, Maps.radial);
            };

            public static Func<float, Func<float, float, float, float, RegionScalarMap>> linear = (a) => (xlo, xhi, ylo, yhi) =>
            {
                return new RegionScalarMap(xlo, xhi, ylo, yhi, Math.Cos(a)*ylo - Math.Sin(a)*xlo, Math.Cos(a)*yhi - Math.Sin(a)*xhi, Maps.linear(a));
            };

            public static Func<float, float, Func<float, float, float, float, RegionScalarMap>> spiral = (r, a) => (xlo, xhi, ylo, yhi) =>
            {
                return new RegionScalarMap(xlo, xhi, ylo, yhi, 0, Math.TWOPI, Maps.spiral(r, a));
            };
        }

        //input information
        public readonly float xlo; public readonly float xhi;
        public readonly float ylo; public readonly float yhi;
        public readonly float zlo; public readonly float zhi;
        public readonly Func<float, float, float> f;
        //contract: the midrange of the image of f under xr cross yr is a reasonably close subset of zr

        public RegionScalarMap(float xlo, float xhi, float ylo, float yhi, float zlo, float zhi, Func<float, float, float> f)
        {
            if (xhi < xlo) { this.xlo = xhi; this.xhi = xlo; } else { this.xlo = xlo; this.xhi = xhi; }
            if (yhi < ylo) { this.ylo = yhi; this.yhi = ylo; } else { this.ylo = ylo; this.yhi = yhi; }
            if (zhi < zlo) { this.zlo = zhi; this.zhi = zlo; } else { this.zlo = zlo; this.zhi = zhi; }
            this.f = f;
        }

        public static float PositiveArgument(float x, float y)
        {
            var t = Math.Atan2(x, y);
            return t < 0 ? Math.PI - t : t;
        }

        public static float MagnitudeSquared(float x, float y)
        {
            return x * x + y * y;
        }

        public static float Magnitude(float x, float y)
        {
            return Math.Sqrt(MagnitudeSquared(x, y));
        }

        public static float MaxBound(float x, float y)
        {
            return Math.Max(Math.Abs(x), Math.Abs(y));
        }

        public static float MinBound(float x, float y)
        {
            return Math.Min(Math.Abs(x), Math.Abs(y));
        }

        public static float Modulo(float x, float y)
        {
            return x - y * Math.Floor(x / y);
        }

        public static int SplitRange(int n, float lo, float hi, float x)
        {
            if (n <= 0) throw new ArgumentException("Number of parts must be positive.", nameof(n));
            if (hi <= lo) throw new ArgumentException("High terminal must be greater than low terminal.");
            var d = x - lo;
            var m = hi - lo;
            var mn = Math.FloorToInt(m / n);
            if (mn == 0) return 0;
            var i = Math.FloorToInt(d / mn);
            return Math.Min(Math.Max(0, i), n - 1);
        }
    }

    public static class ListExt
    {
        public static T SplitRange<T>(this List<T> list, float lo, float hi, float x)
        {
            return list[RegionScalarMap.SplitRange(list.Count, lo, hi, x)];
        }

        public static T SplitRange<T>(this List<T> list, RegionScalarMap map, float x, float y)
        {
            return SplitRange(list, map.zlo, map.zhi, map.f(x,y));
        }
    }
}
