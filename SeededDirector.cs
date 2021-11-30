using System;
using System.Collections.Generic;
using System.Linq;
using static Combloonation.Labloontory;
using Assets.Scripts.Models.Rounds;
using Assets.Scripts.Models.Bloons;
using BTD_Mod_Helper.Extensions;
using MelonLoader;

namespace Combloonation
{
    public class SeededDirector
    {
        public readonly Random random;
        public readonly int seed;

        public SeededDirector(int seed)
        {
            random = new Random(seed);
            this.seed = seed;
        }

        public SeededDirector() : this(new Random().Next())
        {

        }

        public BloonGroupModel[] Split(BloonGroupModel group, int size, out int excess)
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

        public BloonGroupModel[] Split(BloonGroupModel[] roundGroups, int[] sizes)
        {
            return Split(roundGroups, sizes, bloons => Fuse(bloons));
        }

        public BloonGroupModel[] Split(BloonGroupModel[] roundGroups, int[] sizes, Func<List<BloonModel>, BloonModel> fuser)
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

        public void MutateRounds()
        {
            MelonLogger.Msg("Mutating rounds...");
            foreach (RoundSetModel round in GetGameModel().roundSets)
            {
                foreach (var rounds in round.rounds.Take(50))
                {
                    var size = rounds.groups.Sum(g => g.count);
                    var parts = random.Next(1, size + 1);
                    rounds.groups = Split(rounds.groups, Partition(size, parts));
                }
            }
        }
    }
}
