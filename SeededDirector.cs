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

namespace Combloonation
{

    public enum Directable
    {
        [EnumMember(Value = "GameModel")]
        GameModel,
        [EnumMember(Value = "RoundSetModel")]
        RoundSetModel,
        [EnumMember(Value = "RoundModel")]
        RoundModel,
        [EnumMember(Value = "BloonGroupModel")]
        BloonGroupModel,
        [EnumMember(Value = "BloonModel")]
        BloonModel,
        [EnumMember(Value = "FreeplayBloonGroupModel")]
        FreeplayBloonGroupModel,
        [EnumMember(Value = "BloonEmissionModel")]
        BloonEmissionModel,
    }

    public interface IDirector
    {
        float Eval(GameModel model);
        float Eval(RoundSetModel model);
        float Eval(BloonGroupModel model);
        float Eval(RoundModel model);
        float Eval(BloonModel model);
        float Eval(FreeplayBloonGroupModel model);
        float Eval(BloonEmissionModel model);

        SortedList<float, Model> Produce(Directable d, float? v, int n = 1);
    }

    public abstract class SeededDirector : IDirector
    {
        public readonly Random random;
        public readonly int seed;

        public SeededDirector(int seed)
        {
            random = new Random(seed);
            this.seed = seed;
        }

        public SeededDirector() : this(new Random().Next()) { }

        public abstract float Eval(GameModel model);
        public abstract float Eval(RoundSetModel model);
        public abstract float Eval(BloonGroupModel model);
        public abstract float Eval(RoundModel model);
        public abstract float Eval(BloonModel model);
        public abstract float Eval(FreeplayBloonGroupModel model);
        public abstract float Eval(BloonEmissionModel model);

        public abstract SortedList<float, Model> Produce(Directable d, float? v, int n = 1);
    }

    public class RoundMutatorDirector : SeededDirector
    {
        public static GameModel produced;

        public RoundMutatorDirector(int seed) : base(seed) { }
        public RoundMutatorDirector() : base() { }

        public static BloonGroupModel[] Split(BloonGroupModel group, int size, out int excess)
        {
            var first = group.Duplicate();
            var span = group.count;
            excess = size - span;
            if (size <= 0 || size >= span) return new BloonGroupModel[] { first };
            var last = group.Duplicate();
            var step = size == 1 ? 0 : (group.end - group.start) / (span - 1);
            last.start = (first.end = group.start + size * step) + step;
            return new BloonGroupModel[] { first, last };
        }

        public static BloonGroupModel[] Split(BloonGroupModel[] roundGroups, int[] sizes)
        {
            return Split(roundGroups, sizes, bloons => Fuse(bloons));
        }

        public static BloonGroupModel[] Split(BloonGroupModel[] roundGroups, int[] sizes, Func<List<BloonModel>, BloonModel> fuser)
        {
            var groups = new List<BloonGroupModel>();
            var subgroups = new List<BloonGroupModel>();
            var bloons = new List<BloonModel>();
            var i = 0; var size = sizes[i];
            var j = 0; var group = roundGroups[j];
            while (i < sizes.Length && j < roundGroups.Length)
            {
                bloons.Add(GetBloonByName(group.bloon));
                var split = Split(group, size, out size);
                subgroups.Add(split.First());
                if (size > 0)
                {
                    if (++j < roundGroups.Length) group = roundGroups[j];
                    continue;
                }
                if (size == 0)
                {
                    group = split.Last();
                }
                else
                {
                    if (++j < roundGroups.Length) group = roundGroups[j];
                }

                if (++i < sizes.Length) size = sizes[i];

                var bloon = fuser(bloons);
                foreach (var subgroup in subgroups)
                {
                    subgroup.bloon = bloon.id;
                    groups.Add(subgroup);
                }
                bloons.Clear();
                subgroups.Clear();
            }
            return groups.ToArray();
        }

        public override float Eval(GameModel model) { return 0f; }
        public override float Eval(RoundSetModel model) { return 0f; }
        public override float Eval(BloonGroupModel model) { return 0f; }
        public override float Eval(RoundModel model) { return 0f; }
        public override float Eval(BloonModel model) { return 0f; }
        public override float Eval(FreeplayBloonGroupModel model) { return 0f; }
        public override float Eval(BloonEmissionModel model) { return 0f; }


        public override SortedList<float, Model> Produce(Directable d, float? v, int n = 1)
        {
            if (d != Directable.GameModel) throw new NotImplementedException();

            var game = produced ?? GetGameModel();
            if (produced == null)
            {
                MelonLogger.Msg("Mutating rounds...");

                foreach (RoundSetModel roundSet in game.roundSets)
                {
                    foreach (var round in roundSet.rounds)
                    {
                        var size = round.groups.Sum(g => g.count);
                        var parts = random.Next(1, size + 1);
                        round.groups = Split(round.groups, Partition(size, parts));
                    }
                }
                produced = game;
            }

            var list = new SortedList<float, Model>(1);
            list.Add(0f, game);
            return list;
        }

    }

    public class RandomDirector : SeededDirector
    {
        public RandomDirector(int seed) : base(seed) { }

        public override float Eval(GameModel model) { return (float)random.NextDouble(); }

        public override float Eval(RoundSetModel model) { return (float)random.NextDouble(); }

        public override float Eval(RoundModel model) { return (float)random.NextDouble(); }
        public override float Eval(BloonGroupModel model) { return (float)random.NextDouble(); }

        public override float Eval(BloonModel model) { return (float)random.NextDouble(); }

        public override float Eval(FreeplayBloonGroupModel model) { return (float)random.NextDouble(); }

        public override float Eval(BloonEmissionModel model) { return (float)random.NextDouble(); }

        public override SortedList<float, Model> Produce(Directable d, float? v, int n = 1)
        {
            SortedList<float, Model> list = new SortedList<float, Model> { };
            var game = GetGameModel();
            var bloons = game.bloons;
            switch (d)
            {
                case Directable.BloonModel:
                    var parts = random.Next(1, n);
                    var partition = Partition(n, parts, random);
                    var choice = bloons.Shuffle(random).Take(parts);
                    var i = 0;
                    foreach (var bloon in choice.SelectMany(b => Enumerable.Repeat(b, partition[i++])))
                    {
                        list.Add(Eval(bloon), bloon);
                    }
                    break;
                case Directable.BloonGroupModel:
                    break;
                case Directable.RoundModel:
                    break;
                case Directable.RoundSetModel:
                    break;
                case Directable.GameModel:
                    break;
                case Directable.BloonEmissionModel:
                    break;
                case Directable.FreeplayBloonGroupModel:
                    break;
            }
            return list;
        }
    }

    public class TestDirector : SeededDirector
    {
        public TestDirector(int seed) : base(seed) { }

        public override float Eval(GameModel model)
        {
            return model.roundSets.Sum(r => Eval(r)) + model.freeplayGroups.Sum(g => Eval(g));
        }

        public override float Eval(RoundSetModel model)
        {
            return model.rounds.Sum(r => Eval(r));
        }

        public override float Eval(RoundModel model)
        {
            return model.emissions.GroupBy(e => e.bloon).Count() + model.emissions.Sum(e => Eval(e)) + model.groups.GroupBy(g => g.bloon).Count() + model.groups.Sum(g => Eval(g));
        }

        public override float Eval(BloonGroupModel model)
        {
            return Eval(GetBloonByName(model.bloon)) * model.count;// / (model.end - model.start);
        }

        public override float Eval(BloonModel model)
        {
            return /*model.tags.Count + model.behaviors.Count + model.speed + model.maxHealth +*/ model.childBloonModels.ToList().GroupBy(b => b).Count() + model.childBloonModels.ToList().Sum(b => Eval(b));
        }

        public override float Eval(FreeplayBloonGroupModel model)
        {
            return /*model.score + */ Eval(model.group) + model.bloonEmissions.GroupBy(e => e.bloon).Count() + model.bloonEmissions.Sum(e => Eval(e));
        }

        public override float Eval(BloonEmissionModel model)
        {
            return Eval(GetBloonByName(model.bloon));// / model.time;
        }

        public override SortedList<float, Model> Produce(Directable d, float? v, int n = 1)
        {
            if (d != Directable.GameModel) throw new NotImplementedException();

            var model = GetGameModel();
            var list = new SortedList<float, Model>(1);
            list.Add(Eval(model), model);
            return list;
        }
    }

    public class DangerDirector : SeededDirector
    {
        public DangerDirector(int seed) : base(seed) { }

        public override float Eval(GameModel model)
        {
            return model.roundSets.Sum(r => Eval(r)) + model.freeplayGroups.Sum(g => Eval(g));
        }

        public override float Eval(RoundSetModel model)
        {
            return model.rounds.Sum(r => Eval(r));
        }

        public override float Eval(RoundModel model)
        {
            return model.emissions.Sum(e => Eval(e)) + model.groups.Sum(g => Eval(g));
        }

        public override float Eval(BloonGroupModel model)
        {
            return Eval(GetBloonByName(model.bloon)) * model.count;
        }

        public override float Eval(BloonModel model)
        {
            return model.danger;
        }

        public override float Eval(FreeplayBloonGroupModel model)
        {
            return Eval(model.group) + model.bloonEmissions.Sum(e => Eval(e));
        }

        public override float Eval(BloonEmissionModel model)
        {
            return Eval(GetBloonByName(model.bloon));
        }

        public override SortedList<float, Model> Produce(Directable d, float? v, int n = 1)
        {
            SortedList<float, Model> list = new SortedList<float, Model> { };
            var game = GetGameModel();
            var bloons = game.bloons.Select(b => (int)Eval(b)).ToArray();
            switch (d)
            {
                case Directable.BloonModel:
                    var choice = ArgUnbounded1DKnapsack((int)v, bloons).GroupBy(i => i);
                    var fusion = Fuse(choice.Select(g => game.bloons[g.Key]));
                    var value = Eval(fusion);
                    var count = choice.Sum(g => g.Count());
                    for (int i = 0; i < count; i++) list.Add(value, fusion);
                    break;
                case Directable.BloonGroupModel:
                    break;
                case Directable.RoundModel:
                    break;
                case Directable.RoundSetModel:
                    break;
                case Directable.GameModel:
                    break;
                case Directable.BloonEmissionModel:
                    break;
                case Directable.FreeplayBloonGroupModel:
                    break;
            }
            return list;
        }
    }

}