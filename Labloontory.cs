using System.Linq;
using Assets.Scripts.Models.Bloons;
using Assets.Scripts.Models.Rounds;
using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;
using Assets.Scripts.Models.Bloons.Behaviors;
using MelonLoader;
using System;
using Assets.Scripts.Unity;
using Random = System.Random;

namespace Combloonation
{

    public static class Labloontory
    {

        public static readonly Random random = new Random();
        public static Il2CppSystem.Collections.Generic.Dictionary<string, BloonModel> lookup;

        public static string BloonsToId(IEnumerable<BloonModel> bloons)
        {
            return string.Join("_", bloons.Select(f => f.id));
        }

        public static IEnumerable<BloonModel> BloonsFromId(string id)
        {
            return id.Split('_').Select(s => lookup[s]);
        }

        public static IEnumerable<string> MinimalBloonIdsFromId(string id)
        {
            return id.Replace("Fortified", "").Replace("Camo", "").Replace("Regrow", "").Split('_').Distinct();
        }

        public class BloonsionReactor
        {
            public readonly IEnumerable<BloonModel> fusands;
            public readonly BloonModel fusion;
            public bool real = false;

            public BloonsionReactor(IEnumerable<BloonModel> bloons)
            {

                fusands = bloons.Select(b => b.id).Distinct().Select(s => lookup[s]).OrderByDescending(f => f.danger);
                fusion = Clone(fusands.First());
                // assume that the most 'danger' is the best pick for the display (and max danger can be left unset)
            }

            public BloonsionReactor Merge()
            {
                real = true;
                return MergeId().MergeProperties().MergeHealth().MergeSpeed().MergeDisplay().MergeBehaviors();//.MergeChildren();
            }

            public BloonsionReactor MergeId()
            {
                fusion.id = BloonsToId(fusands);
                fusion.baseId = fusion.id;
                if (real) MelonLogger.Msg("Creating " + fusion.id + ":");
                return this;
            }

            public BloonsionReactor MergeProperties()
            {
                fusion.bloonProperties = fusands.Select(f => f.bloonProperties).Aggregate((a, b) => a | b);

                fusion.isBoss = fusands.Any(f => f.isBoss);
                fusion.isCamo = fusands.Any(f => f.isCamo);
                fusion.isFortified = fusands.Any(f => f.isFortified);
                fusion.isGrow = fusands.Any(f => f.isGrow);
                fusion.isMoab = fusands.Any(f => f.isMoab);

                fusion.distributeDamageToChildren = fusands.Any(f => f.distributeDamageToChildren);
                fusion.tags = fusands.SelectMany(f => f.tags).Distinct().ToArray();
                if (real) MelonLogger.Msg("     - " + fusion.tags.Length + " tags");

                return this;
            }

            public BloonsionReactor MergeHealth()
            {
                fusion.maxHealth = fusands.Max(f => f.maxHealth);
                fusion.isInvulnerable = fusands.Any(f => f.isInvulnerable);
                fusion.leakDamage = fusands.Max(f => f.leakDamage);
                fusion.totalLeakDamage = fusands.Max(f => f.totalLeakDamage);
                fusion.loseOnLeak = fusands.Any(f => f.loseOnLeak);
                if (real) MelonLogger.Msg("     - " + fusion.maxHealth + " health");
                return this;
            }

            public BloonsionReactor MergeSpeed()
            {
                fusion.speed = fusands.Max(f => f.speed);
                fusion.speedFrames = fusands.Max(f => f.speed);
                if (real) MelonLogger.Msg("     - " + fusion.speed + " speed");
                return this;
            }

            public BloonsionReactor MergeChildren()
            {
                var fusand_children = fusands.Select(f => f.GetBehavior<SpawnChildrenModel>().children);
                var bound = fusand_children.Max(c => c.Count());
                var children = fusand_children.Select(c => new Combinomial(c)).Aggregate((a, b) => a.Product(b).Cull().Bound(bound));
                if (real) MelonLogger.Msg("     - " + children);
                fusion.GetBehavior<SpawnChildrenModel>().children = children.Terms().SelectMany(p => Enumerable.Repeat(Fuse(p.Key), p.Value)).Select(c => c.id).ToArray();
                return this;
            }

            public BloonsionReactor MergeDisplay()
            {
                fusion.radius = fusands.Max(f => f.radius);
                fusion.rotate = fusands.Any(f => f.rotate);
                fusion.rotateToFollowPath = fusands.Any(f => f.rotateToFollowPath);
                fusion.icon = fusands.First(f => f.icon != null).icon;
                return this;
            }

            public BloonsionReactor MergeBehaviors()
            {
                fusion.behaviors = fusands.SelectMany(f => f.behaviors.ToList()).ToIl2CppReferenceArray();
                fusion.childDependants = fusands.SelectMany(f => f.childDependants.ToList()).ToIl2CppList();
                return this;
            }
        }
        public static BloonModel Fuse(IEnumerable<string> bloons)
        {
            return Fuse(bloons.Select(b => lookup[b]));
        }
        public static BloonModel Fuse(IEnumerable<BloonModel> bloons)
        {
            if (bloons.Count() == 0) return null;
            var reactor = new BloonsionReactor(bloons).MergeId();
            var bloon = reactor.fusion;
            if (lookup.ContainsKey(bloon.id))
            {
                bloon = lookup[bloon.id];
            }
            else
            {
                Register(reactor.Merge().fusion);
            }
            return bloon;
        }

        public static BloonModel Clone(BloonModel bloon)
        {
            return bloon.Clone().Cast<BloonModel>();
        }

        public static BloonModel Register(BloonModel bloon)
        {
            var game = Game.instance.model;
            game.bloons = game.bloons.Prepend(bloon).ToArray();
            game.bloonsByName[bloon.id] = bloon;
            return bloon;
        }

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

        public static BloonGroupModel[] Split(BloonGroupModel[] roundGroups, int[] sizes, Func<BloonModel[], BloonModel> fuser)
        {
            var groups = new List<BloonGroupModel>();
            var subgroups = new List<BloonGroupModel>();
            var bloons = new List<BloonModel>();
            var i = 0; var size = sizes[i];
            var j = 0; var group = roundGroups[j];
            while (i < sizes.Length && j < roundGroups.Length)
            {
                bloons.Add(lookup[group.bloon]);
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
                else {
                    if (++j < roundGroups.Length) group = roundGroups[j];
                }
                
                if (++i < sizes.Length) size = sizes[i];

                var bloon = Fuse(bloons);
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

        //https://stackoverflow.com/a/5807166
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list)
        {
            var r = new Random();
            var shuffledList = list.
                Select(x => new { Number = r.Next(), Item = x }).
                OrderBy(x => x.Number).
                Select(x => x.Item);
            return shuffledList;
        }

        public static int[] Partition(int size, int parts)
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

        public static void MutateRounds()
        {
            MelonLogger.Msg("Mutating rounds...");
            foreach (RoundSetModel round in Game.instance.model.roundSets)
            {
                var i = 1;
                foreach (var rounds in round.rounds)
                {
                    var size = rounds.groups.Sum(g => g.count);
                    var parts = random.Next(1, size + 1);
                    MelonLogger.Msg("Splitting round " + (i++) + " of size " + size + " into " + parts + " parts!");
                    rounds.groups = Split(rounds.groups, Partition(size, parts));
                    //if (i >= 40) break;
                }
            }
        }
    }
}
