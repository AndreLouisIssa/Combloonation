using System;
using System.Collections.Generic;
using System.Linq;
using static Combloonation.Labloontory;
using static Combloonation.Helpers;
using Assets.Scripts.Models.Rounds;
using BTD_Mod_Helper.Extensions;
using Assets.Scripts.Models;
using Bounds = Assets.Scripts.Models.Rounds.FreeplayBloonGroupModel.Bounds;
using HarmonyLib;
using Assets.Scripts.Models.Bloons;

namespace Combloonation
{

    public interface IGoal
    {
        float? strict { get; }
        float? score { get; }
        int? count { get; }
        string[] bloons { get; }
        string props { get; }
    }

    public struct Goal : IGoal
    {
        public float? strict { get; set; }
        public float? score { get; set; }
        public int? count { get; set; }
        public string[] bloons { get; set; }
        public string props { get; set; }
        public Goal(float? strict, float? score, int? count, string[] bloons, string props)
        {
            this.strict = strict; this.score = score; this.count = count; this.bloons = bloons; this.props = props;
        }
    }

    public interface IDirector
    {
        GameModel game { get; }
        float Score(FreeplayBloonGroupModel model);
        float Score(BloonGroupModel model);
        float Score(BloonModel model);
        bool Mutate(IGoal goal = null);
    }

    public abstract class Director : IDirector
    {
        public GameModel game { get; }
        public Director(GameModel game) { this.game = game; }
        public Director() : this(GetGameModel()) { }
        public abstract float Score(FreeplayBloonGroupModel model);
        public abstract float Score(BloonGroupModel model);
        public abstract float Score(BloonModel model);
        public abstract bool Mutate(IGoal goal = null);

    }

    public abstract class SeededDirector : Director
    {
        public readonly Random random;
        public readonly int seed;

        public SeededDirector(GameModel game, int seed) : base(game)
        {
            random = new Random(seed);
            this.seed = seed;
        }

        public SeededDirector(GameModel game) : this(game, new Random().Next()) { }

        public SeededDirector(int seed) : base()
        {
            random = new Random(seed);
            this.seed = seed;
        }

        public SeededDirector() : this(new Random().Next()) { }

    }

    public class MainDirector : SeededDirector
    {
        public MainDirector(GameModel game, int seed) : base(game, seed) { }
        public MainDirector(GameModel game) : base(game) { }
        public MainDirector(int seed) : base(seed) { }
        public MainDirector() : base() { }

        public static FreeplayBloonGroupModel[] Split(FreeplayBloonGroupModel fgroup, int size, out int excess)
        {
            var group = fgroup.group;
            var first = group.Duplicate();
            var span = group.count;
            excess = size - span;
            if (size <= 0 || size >= span)
                return new FreeplayBloonGroupModel[] { new FreeplayBloonGroupModel(fgroup.name, fgroup.score, fgroup.bounds, first) };
            var last = group.Duplicate();
            var step = size == 1 ? 0 : (group.end - group.start) / (span - 1);
            last.start = (first.end = group.start + size * step) + step;
            return new FreeplayBloonGroupModel[] { new FreeplayBloonGroupModel(fgroup.name, fgroup.score, fgroup.bounds, first), new FreeplayBloonGroupModel(fgroup.name, fgroup.score, fgroup.bounds, last) };
        }

        public static FreeplayBloonGroupModel[] Split(FreeplayBloonGroupModel[] inGroups, int[] sizes)
        {
            var fgroups = new List<FreeplayBloonGroupModel>();
            var subfgroups = new List<FreeplayBloonGroupModel>();
            var bloons = new List<string>();
            var i = 0; var size = sizes[i];
            var j = 0; var fgroup = inGroups[j];
            while (i < sizes.Length && j < inGroups.Length)
            {
                bloons.Add(fgroup.group.bloon);
                var split = Split(fgroup, size, out size);
                subfgroups.Add(split.First());
                if (size > 0 && ++j < inGroups.Length) { fgroup = inGroups[j]; continue; }
                if (size == 0) fgroup = split.Last();
                else if (++j < inGroups.Length) fgroup = inGroups[j];
                if (++i < sizes.Length) size = sizes[i];

                var bloon = Fuse(bloons);
                var bounds = subfgroups.SelectMany(f => f.bounds);
                var bound = (bounds.Count() > 0) ? NewBounds(bounds.Min(b => b.lowerBounds), bounds.Max(b => b.upperBounds)) : NewBounds(0, 0);
                foreach (var subgroup in subfgroups)
                {
                    subgroup.bounds = new Bounds[] { bound };
                    subgroup.group.bloon = bloon.name;
                    if (bloon is FusionBloonModel fusion)
                        subgroup.group.count = (int)Math.Ceiling(((double)subgroup.group.count) / fusion.fusands.Count());
                    fgroups.Add(subgroup);
                }
                bloons.Clear();
                subfgroups.Clear();
            }
            return fgroups.ToArray();
        }

        public static FreeplayBloonGroupModel[] Infuse(FreeplayBloonGroupModel[] inGroups, Random random)
        {
            var size = inGroups.Sum(g => g.group.count);
            var ratio = size / inGroups.Length;
            var parts = random.Next(Math.Min(size, ratio), Math.Max(size, ratio));
            return Split(inGroups, Partition(size, parts, random));
        }

        public static void Shift(FreeplayBloonGroupModel f, int shift)
        {
            f.bounds = f.bounds.Select(b => NewBounds(b.lowerBounds + shift, b.upperBounds + shift)).ToArray();
        }

        public static void WidenBelow(FreeplayBloonGroupModel f, int margin)
        {
            f.bounds = f.bounds.Select(b => NewBounds(b.lowerBounds - margin, b.upperBounds)).ToArray();
        }

        public static void WidenAbove(FreeplayBloonGroupModel f, int margin)
        {
            f.bounds = f.bounds.Select(b => NewBounds(b.lowerBounds, b.upperBounds + margin)).ToArray();
        }

        public static void Buff(FreeplayBloonGroupModel f, float scale)
        {
            var g = f.group = f.group.Duplicate(); g.count = (int)(g.count * scale);
        }

        public static void AdjustLeft(FreeplayBloonGroupModel f)
        {
            var g = f.group; g.end -= g.start; g.start = 0;
        }

        public override bool Mutate(IGoal goal = null)
        {
            Dictionary<string, double> bloonChances = new Dictionary<string, double>();
            if (!(goal?.bloons is null)) goal.bloons.Do(b => bloonChances.Add(b, 1 / (1 + Score(BloonFromName(b)))));
            var freeplayGroups = new List<FreeplayBloonGroupModel> { };
            //var roundSets = game.roundSets.Select(rs => { var nrs = rs.Duplicate(); nrs.rounds = rs.rounds.Select(r => r.Duplicate()).ToArray(); return nrs; }).ToList();
            foreach (var roundSet in game.roundSets) for (int j = 0; j < roundSet.rounds.Length; ++j)
            {
                var round = roundSet.rounds[j];
                var groups = round.groups;
                if (groups.Length > 1)
                {
                    groups = Split(groups.Select(g => new RoundBloonGroupModel(g, null)).ToArray(),
                        Partition(groups.Sum(g => g.count), random.Next(1, groups.Length), random)).Select(f => f.group).ToArray();
                }
                if (!(goal?.bloons is null)) foreach (var group in groups)
                {
                    IEnumerable<string> bloons;
                    if (BloonFromName(group.bloon) is FusionBloonModel fusion) bloons = BloonNamesFromBloons(fusion.fusands);
                    else bloons = new string[] { group.bloon };
                    group.bloon = Fuse(bloons.Concat(RandomSubset(bloonChances, ((double)j)/roundSet.rounds.Length, random))).name;
                }
                groups.Do(g => freeplayGroups.Add(new RoundBloonGroupModel(g.Duplicate(), j).Apply(AdjustLeft)));
                //round.groups = groups;
            }
            game.freeplayGroups = freeplayGroups.ToArray();
            var n = goal?.count ?? 2;
            var w = (int)(- (goal?.strict ?? 0.25f)) * 100;
            game.roundSets = game.roundSets.Select(rs => new RoundSetModel(rs.name, rs.rounds.Take(1).ToArray())).ToArray();
            if (!(goal?.props is null))
                game.roundSets.SelectMany(rs => rs.rounds.SelectMany(r => r.groups)).Concat(game.freeplayGroups.Select(f => f.group)).Do(g => g.bloon = Fuse(new string[] { g.bloon }, goal.props).name);
            game.freeplayGroups = game.freeplayGroups.Iterate(l => Infuse(l, random)
                .ApplyEach(t => WidenBelow(t, w), t => Buff(t, 5)).ToArray()).Take(n).SelectMany(s => s).ToArray();
            return true;
        }

        public override float Score(FreeplayBloonGroupModel model)
        {
            return 4 * Score(model.group);
        }

        public override float Score(BloonGroupModel model)
        {
            return 2 * (float)Math.Pow(model.count, 0.75) * Score(BloonFromName(model.bloon)) * (float)Math.Pow(1 + 1/(1 + model.end - model.start), 0.25);
        }

        public override float Score(BloonModel model)
        {
            return (float)(120*(1+GetProperties(model).Count())*(1+model.speed)*(1+Math.Pow(model.maxHealth,0.5)/25)/(25+model.danger));
        }
    }

}