using System;
using System.Collections.Generic;
using System.Linq;
using static Combloonation.Labloontory;
using static Combloonation.Helpers;
using Assets.Scripts.Models.Rounds;
using Assets.Scripts.Models.Bloons;
using BTD_Mod_Helper.Extensions;
using MelonLoader;
using Assets.Scripts.Models;
using System.Runtime.Serialization;
using UnhollowerRuntimeLib;
using Bounds = Assets.Scripts.Models.Rounds.FreeplayBloonGroupModel.Bounds;
using UnhollowerBaseLib;
using HarmonyLib;

namespace Combloonation
{

    public interface IGoal
    {
        float? score { get; }
        int? count { get; }
        string[] bloons { get; }
    }

    public interface IDirector
    {
        GameModel game { get; }

        float? Score(FreeplayBloonGroupModel model);

        Tuple<RoundSetModel[], FreeplayBloonGroupModel[]> Produce(IGoal goal = null);
    }

    public abstract class Director : IDirector
    {
        public GameModel game { get; }

        public Director(GameModel game)
        {
            this.game = game;
        }

        public Director() : this(GetGameModel()) { }

        public abstract float? Score(FreeplayBloonGroupModel group);

        public abstract Tuple<RoundSetModel[], FreeplayBloonGroupModel[]> Produce(IGoal goal);

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
            var ratio = size/inGroups.Length;
            var parts = random.Next(Math.Min(size, ratio), Math.Max(size, ratio));
            return Split(inGroups, Partition(size, parts, random));
        }

        public static void Shift(FreeplayBloonGroupModel[] inGroups, int shift)
        {
            inGroups.Do(f => f.bounds = f.bounds.Select(b => NewBounds(b.lowerBounds + shift, b.upperBounds + shift)).ToArray());
        }

        public static void Widen(FreeplayBloonGroupModel[] inGroups, int margin)
        {
            inGroups.Do(f => f.bounds = f.bounds.Select(b => NewBounds(b.lowerBounds - margin, b.upperBounds + margin)).ToArray());
        }

        public static void Buff(FreeplayBloonGroupModel[] inGroups, float scale)
        {
            inGroups.Do(f => { var g = f.group = f.group.Duplicate(); g.count = (int)(g.count*scale); });
        }

        public static void Adjust(IEnumerable<FreeplayBloonGroupModel> inGroups)
        {
            inGroups.Do(f => { var g = f.group; g.end -= g.start; g.start = 0; });
        }

        public override Tuple<RoundSetModel[], FreeplayBloonGroupModel[]> Produce(IGoal goal = null)
        {
            var freeplayGroups = new List<FreeplayBloonGroupModel> { };
            var roundSets = game.roundSets.Select(rs => { var nrs = rs.Duplicate(); nrs.rounds = rs.rounds.Select(r => r.Duplicate()).ToArray(); return nrs; }).ToList();
            foreach (var roundSet in roundSets) for (int j = 0; j < roundSet.rounds.Length; ++j)
            {
                var round = roundSet.rounds[j];
                var groups = round.groups; 
                if (groups.Length > 1) {
                    groups = Split(groups.Select(g => new RoundBloonGroupModel(g, null)).ToArray(),
                        Partition(groups.Sum(g => g.count), random.Next(1, groups.Length), random)).Select(f => f.group).ToArray();
                }
                groups.Do(g => freeplayGroups.Add(new RoundBloonGroupModel(g, j)));
                round.groups = groups;
            }
            freeplayGroups = freeplayGroups.ToArray().Iterate(l => Infuse(l, random)
                .Apply(t => Shift(t, 100), t => Widen(t, 50), t => Buff(t, 2))).Take(3).SelectMany(s => s).Apply(Adjust).ToList();
            return new Tuple<RoundSetModel[], FreeplayBloonGroupModel[]>( roundSets.ToArray(), freeplayGroups.ToArray());
        }

        public override float? Score(FreeplayBloonGroupModel model) { return null; }
    }

}