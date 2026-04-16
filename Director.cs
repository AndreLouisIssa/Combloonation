using System;
using System.Collections.Generic;
using static Combloonation.Labloontory;
using static Combloonation.Helpers;
using Il2CppAssets.Scripts.Models.Rounds;
using Il2CppAssets.Scripts.Models;
using Bounds = Il2CppAssets.Scripts.Models.Rounds.FreeplayBloonGroupModel.Bounds;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Bloons;
using Il2CppAssets.Scripts.Data;

namespace Combloonation
{

    public record struct Goal(float? Strict, float? Score, int? Count, string[] Bloons, string Props);

    public interface IDirector
    {
        float Score(FreeplayBloonGroupModel model);
        float Score(BloonGroupModel model);
        float Score(BloonModel model);
        bool Mutate(Goal? goal = null);
    }

    public abstract class Director(GameModel gameModel, GameData gameData) : IDirector
    {
        public GameModel GameModel { get; } = gameModel; public GameData GameData { get; } = gameData;

        public Director() : this(GetGameModel(), GetGameData()) { }
        public abstract float Score(FreeplayBloonGroupModel model);
        public abstract float Score(BloonGroupModel model);
        public abstract float Score(BloonModel model);
        public abstract bool Mutate(Goal? goal = null);

    }

    public abstract class SeededDirector : Director
    {
        public readonly Random random;
        public readonly int seed;

        public SeededDirector(GameModel gameModel, GameData gameData, int seed) : base(gameModel, gameData)
        {
            random = new Random(seed);
            this.seed = seed;
        }

        public SeededDirector(GameModel gameModel, GameData gameData) : this(gameModel, gameData, new Random().Next()) { }

        public SeededDirector(int seed) : base()
        {
            random = new Random(seed);
            this.seed = seed;
        }

        public SeededDirector() : this(new Random().Next()) { }

    }

    public class MainDirector : SeededDirector
    {
        public MainDirector(GameModel gameModel, GameData gameData, int seed) : base(gameModel, gameData, seed) { }
        public MainDirector(GameModel gameModel, GameData gameData) : base(gameModel, gameData) { }
        public MainDirector(int seed) : base(seed) { }
        public MainDirector() : base() { }

        public static float Weight(BloonModel b) => b.danger + b.tags.Length + Bits((uint)b.bloonProperties) + 1;

        public static float Weight(BloonModel a, BloonModel b) => Weight(a) - Weight(b) + 1;

        public static FreeplayBloonGroupModel[] Split(FreeplayBloonGroupModel fgroup, int size, out int excess)
        {
            var group = fgroup.group;
            var first = group.Duplicate();
            var span = group.count;
            excess = size - span;
            if (size <= 0 || size >= span)
                return [new FreeplayBloonGroupModel(fgroup.name, fgroup.score, fgroup.bounds, first)];
            var last = group.Duplicate();
            var step = size == 1 ? 0 : (group.end - group.start) / (span - 1);
            last.start = (first.end = group.start + size * step) + step;
            return [new FreeplayBloonGroupModel(fgroup.name, fgroup.score, fgroup.bounds, first), new FreeplayBloonGroupModel(fgroup.name, fgroup.score, fgroup.bounds, last)];
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
                var bound = (bounds.Any()) ? NewBounds(bounds.Min(b => b.lowerBounds), bounds.Max(b => b.upperBounds)) : NewBounds(0, 0);
                foreach (var subgroup in subfgroups)
                {
                    subgroup.bounds = new Bounds[] { bound };
                    var oldBloon = BloonFromName(subgroup.group.bloon);
                    subgroup.group.bloon = bloon.name;
                    subgroup.group.count = (int)Math.Ceiling(subgroup.group.count / Weight(bloon, oldBloon));
                    fgroups.Add(subgroup);
                }
                bloons.Clear();
                subfgroups.Clear();
            }
            return [.. fgroups];
        }

        public static FreeplayBloonGroupModel[] Infuse(FreeplayBloonGroupModel[] inGroups, Random random)
        {
            var size = inGroups.Sum(g => g.group.count);
            var ratio = size / inGroups.Length;
            var parts = random.Next(Math.Min(size, ratio), Math.Max(size, ratio));
            return Split(inGroups, Partition(size, parts, random));
        }

        public static void AdjustLeft(FreeplayBloonGroupModel f)
        {
            var g = f.group; g.end -= g.start; g.start = 0;
        }

        public static bool ValidRoundSet(RoundSetModel roundSet)
        {
            return roundSet.name == "DefaultRoundSet" || roundSet.name == "AlternateRoundSet";
        }

        public override bool Mutate(Goal? goal = null)
        {
            var roundSets = GameData.roundSets.Where(ValidRoundSet).ToArray();
            var roundBloons = roundSets.SelectMany(rs => rs.rounds.SelectMany(r => r.groups.Select(g => g.bloon))).Distinct().OrderByDescending(b => Score(BloonFromName(b))).ToList();
            Dictionary<string, double> bloonChances = [];
            goal?.Bloons?.Do(b => bloonChances.Add(b, 1 / (1 + Score(BloonFromName(b)))));
            var freeplayGroups = new List<FreeplayBloonGroupModel> { };
            var roundSet = roundSets.ArgMax(rs => rs.rounds.Length).Duplicate();
            var roundsCount = roundSet.rounds.Length;
            for (int j = 0; j < roundsCount; ++j)
            {
                var groups = roundSets.Where(rs => j >= roundSet.rounds.Length - rs.rounds.Length).Select(rs => rs.rounds[j - roundSet.rounds.Length + rs.rounds.Length]).SelectMany(r => r.groups).ToArray();
                if (groups.Length > 1)
                {
                    groups = [.. Split([.. groups.Select(g => RoundBloonGroupModel(g, null))],
                        Partition(groups.Sum(g => g.count), random.Next(1, groups.Length), random)).Select(f => f.group)];
                }
                if (goal?.Bloons is not null) foreach (var group in groups)
                {
                    IEnumerable<string> bloons;
                    var fusion = FusionFromNameSafe(group.bloon);
                    if (fusion != null) bloons = BloonNamesFromBloons(fusion.fusands);
                    else bloons = [group.bloon];
                    group.bloon = Fuse(bloons.Concat(RandomSubset(bloonChances, ((double)j)/roundsCount, random))).name;
                }
                var goalProps = goal?.Props;
                if (goalProps is not null) foreach (var group in groups) group.bloon = Fuse([group.bloon], goalProps).name;
                groups.Do(g => freeplayGroups.Add(RoundBloonGroupModel(g.Duplicate(), j, roundsCount + 1).Apply(AdjustLeft)));
                roundSet.rounds[j].groups = groups;
            }
            GameModel.freeplayGroups = freeplayGroups.ToArray();
            // TODO: avoid mutating the base roundsets in the future
            for (int i = 0; i < GameData.roundSets.Count; i++)
            {
                var ors = GameData.roundSets[i];
                if (!ValidRoundSet(ors)) continue;
                var nrs = roundSet.Duplicate();
                nrs.name = ors.name;
                GameData.roundSets[i] = nrs;
            }
            //GameModel.roundSet = new RoundSetModel(GameModel.roundSet.name, roundSet.rounds, roundSet.linkedIncomeSet);
            return true;
        }

        public override float Score(FreeplayBloonGroupModel model)
        {
            return Score(model.group);
        }

        public override float Score(BloonGroupModel model)
        {
            return (float)Math.Pow(model.count, 0.75) * Score(BloonFromName(model.bloon)) * (float)Math.Pow(1 + 1/(1 + model.end - model.start), 0.25);
        }

        public override float Score(BloonModel model)
        {
            return (float)((1+GetProperties(model).Count())*(1+model.speed)*(1+Math.Pow(model.maxHealth,0.5)/10)*model.danger);
        }
    }

}