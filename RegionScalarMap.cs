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
            public static Func<float, float, float> radial = (x, y) => PositiveArgument(x, y) % Math.TWOPI;
            public static Func<float, Func<float, float, float>> spiralArchimedian = r => (x, y) =>
            {
                return (r * Magnitude(x, y) + PositiveArgument(x, y)) % Math.TWOPI;
            };
            public static Func<float, Func<float, float, float>> spiralLogarithmic = r => (x, y) =>
            {
                return (-r * (float)System.Math.Log(Magnitude(x, y)) + PositiveArgument(x, y)) % Math.TWOPI;
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

            public static Func<float, Func<float, float, float, float, RegionScalarMap>> spiralArchimedian = r => (xlo, xhi, ylo, yhi) =>
            {
                return new RegionScalarMap(xlo, xhi, ylo, yhi, 0, Math.TWOPI, Maps.spiralArchimedian(r));
            };

            public static Func<float, Func<float, float, float, float, RegionScalarMap>> spiralLogarithmic = r => (xlo, xhi, ylo, yhi) =>
            {
                return new RegionScalarMap(xlo, xhi, ylo, yhi, 0, Math.TWOPI, Maps.spiralLogarithmic(r));
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
            if (zhi < zlo) { this.zlo = zhi; this.zhi = zlo; } else { this.zlo = zlo; this.zhi = xhi; }
            this.f = f;
        }

        public static float PositiveArgument(float x, float y)
        {
            var t = Math.Atan2(x, y);
            return t < 0 ? Math.TWOPI - t : t;
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

        public static int SplitRange(int n, float lo, float hi, float x)
        {
            if (hi == lo) return 0;
            var d = x - lo;
            var m = hi - lo;
            var mn = (int)m / n;
            var i = (int)d / mn;
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
