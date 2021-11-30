using System;
using System.Collections.Generic;
using System.Linq;
using static Combloonation.Labloontory;
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
        [EnumMember(Value = "FreeplayBloonGroupModel")]
        FreeplayBloonGroupModel
    }

    public interface IDirector
    {
        float Eval(GameModel model);
        float Eval(RoundSetModel model);
        float Eval(BloonGroupModel model);
        float Eval(RoundModel model);
        float Eval(FreeplayBloonGroupModel model);

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
        public abstract float Eval(FreeplayBloonGroupModel model);
        public abstract SortedList<float, Model> Produce(Directable d, float? v, int n = 1);
    }

    public class RoundMutator : SeededDirector
    {
        public static GameModel produced;

        public RoundMutator(int seed) : base(seed) { }
        public RoundMutator() : base() { }

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

        public int[] Partition(int size, int parts)
        {
            var pivots = new HashSet<int>(Enumerable.Repeat(0, parts - 1).Select(z => random.Next(1, size)).Append(0).Append(size));
            var sizes = new List<int> { };
            size = pivots.First();
            foreach (var pivot in pivots.Skip(1))
            {
                sizes.Add(pivot - size);
                size = pivot;
            }
            return sizes.ToArray();
        }

        public override float Eval(GameModel model) { return 0f; }

        public override float Eval(RoundSetModel model) { return 0f; }

        public override float Eval(BloonGroupModel model) { return 0f; }

        public override float Eval(RoundModel model) { return 0f; }

        public override float Eval(FreeplayBloonGroupModel model) { return 0f; }

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
}
