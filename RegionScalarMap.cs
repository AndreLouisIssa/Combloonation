using Assets.Scripts.Simulation.SMath;
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
            public static Func<float, float, float> annular = (x, y) => MagnitudeSquared(x, y);
            public static Func<float, float, float> radial = (x, y) => PositiveArgument(x, y);
            public static Func<float, Func<float, float, float>> spiralArchimedian = r => (x, y) =>
            {
                return (r * Math.Sqrt(MagnitudeSquared(x, y)) + PositiveArgument(x, y)) % Math.TWOPI;
            };
            public static Func<float, Func<float, float, float>> spiralLogarithmic = r => (x, y) =>
            {
                return (-r * (float)System.Math.Log(Math.Sqrt(MagnitudeSquared(x, y))) + PositiveArgument(x, y)) % Math.TWOPI;
            };
        }

        public static class Regions
        {
            public static Func<float, float, float, float, RegionScalarMap> vertical = (xrx, xry, yrx, yry) =>
            {
                return new RegionScalarMap(xrx, xry, yrx, yry, yrx, yry, Maps.vertical);
            };

            public static Func<float, float, float, float, RegionScalarMap> horizontal = (xrx, xry, yrx, yry) =>
            {
                return new RegionScalarMap(xrx, xry, yrx, yry, xrx, xry, Maps.horizontal);
            };

            public static Func<float, float, float, float, RegionScalarMap> annular = (xrx, xry, yrx, yry) =>
            {
                var zrx = MagnitudeSquared(MinBound(xrx, xry), MinBound(yrx, yry));
                var zry = MagnitudeSquared(MaxBound(xrx, xry), MaxBound(yrx, yry));
                return new RegionScalarMap(xrx, xry, yrx, yry, zrx, zry, Maps.annular);
            };

            public static Func<float, float, float, float, RegionScalarMap> radial = (xrx, xry, yrx, yry) =>
            {
                return new RegionScalarMap(xrx, xry, yrx, yry, 0, Math.TWOPI, Maps.radial);
            };

            public static Func<float, Func<float, float, float, float, RegionScalarMap>> spiralArchimedian = r => (xrx, xry, yrx, yry) =>
            {
                return new RegionScalarMap(xrx, xry, yrx, yry, 0, Math.TWOPI, Maps.spiralArchimedian(r));
            };

            public static Func<float, Func<float, float, float, float, RegionScalarMap>> spiralLogarithmic = r => (xrx, xry, yrx, yry) =>
            {
                return new RegionScalarMap(xrx, xry, yrx, yry, 0, Math.TWOPI, Maps.spiralLogarithmic(r));
            };
        }

        //input information
        public readonly float xrx;
        public readonly float xry;
        public readonly float yrx;
        public readonly float yry;
        public readonly float zrx;
        public readonly float zry;
        public readonly Func<float, float, float> f;
        //contract: the midrange of the image of f under xr cross yr is a reasonably close subset of zr
        //          for xr, yr, zr it must be that x < y

        public RegionScalarMap(float xrx, float xry, float yrx, float yry, float zrx, float zry, Func<float, float, float> f)
        {

            if (xry < xrx) { this.xrx = xry; this.xry = xrx; }
            else { this.xrx = xrx; this.xry = xry; }
            if (yry < yrx) { this.yrx = yry; this.yry = yrx; }
            else { this.yrx = yrx; this.yry = yry; }
            if (zry < zrx) { this.zrx = zry; this.zry = zrx; }
            else { this.zrx = zrx; this.zry = xry; }

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

        public static float MaxBound(float x, float y)
        {
            return Math.Max(Math.Abs(x), Math.Abs(y));
        }

        public static float MinBound(float x, float y)
        {
            return Math.Min(Math.Abs(x), Math.Abs(y));
        }
    }

    public static class RSMHelperExts
    {
        public static int SplitRange(int n, float lo, float hi, float x)
        {
            if (hi == lo) return 0;
            var d = x - lo;
            var m = hi - lo;
            var mn = (int) m / n;
            var i = (int) d / mn;
            return Math.Min(Math.Max(0, i), n - 1);
        }

        public static T SplitRange<T>(this List<T> list, float lo, float hi, float x)
        {
            return list[SplitRange(list.Count, lo, hi, x)];
        }

        public static T SplitRange<T>(this List<T> list, RegionScalarMap map, float x, float y)
        {
            return SplitRange(list, map.zrx, map.zry, map.f(x,y));
        }
    }
}
