using System.Linq;
using Assets.Scripts.Models.Bloons;
using Assets.Scripts.Models.Rounds;
using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;
using Assets.Scripts.Models.Bloons.Behaviors;
using MelonLoader;
using System;
using Assets.Scripts.Unity;

namespace Combloonation
{

    public static class Labloontory
    {

        public static readonly Random random = new Random();
        public static Il2CppSystem.Collections.Generic.Dictionary<string, BloonModel> lookup;

        public static string BloonString(IEnumerable<BloonModel> bloons)
        {
            return string.Join("_", bloons.Select(f => f.id));
        }
        public class Bloonomial {

            public readonly Dictionary<HashSet<string>, int> terms = new Dictionary<HashSet<string>, int>(HashSet<string>.CreateSetComparer());

            public Bloonomial(IEnumerable<string> bloons = null)
            {
                if (bloons == null) return;
                terms[new HashSet<string> { }] = 1;
                foreach (string bloon in bloons)
                {
                    var k = new HashSet<string> { bloon };
                    terms.TryGetValue(k, out int d);
                    terms[k] = 1 + d;
                }
            }
            public Bloonomial Product(Bloonomial p, bool cull = false)
            {
                //polynomial product
                var r = new Bloonomial();
                foreach (var i in terms.Keys)
                {
                    foreach (var j in p.terms.Keys)
                    {
                        var k = new HashSet<string>(i.Concat(j));
                        r.terms.TryGetValue(k, out int d);
                        r.terms[k] = terms[i] * p.terms[j] + d;
                    }
                }
                if (cull)
                {
                    //cull lower order terms
                    var rem = new List<HashSet<string>>();
                    int n = r.terms.Keys.Max(k => k.Count);
                    foreach (var k in r.terms.Keys)
                    {
                        if (k.Count > 0 && k.Count < n)
                        {
                            rem.Add(k);
                        }
                    }
                    foreach (var k in rem)
                    {
                        r.terms.Remove(k);
                    }
                }
                return r;
            }
        }

        public class BloonsionReactor
        {
            public readonly IEnumerable<BloonModel> fusands;
            public readonly BloonModel fusion;

            public BloonsionReactor(IEnumerable<BloonModel> bloons)
            {

                fusands = new HashSet<string>(bloons.Select(b => b.id)).Select(s => lookup[s]);
                fusion = Clone(fusands.First());
            }

            public BloonsionReactor Merge()
            {
                return MergeId().MergeProperties().MergeHealth().MergeSpeed().MergeDisplay().MergeBehaviors().MergeChildren();
            }

            public BloonsionReactor MergeId()
            {
                fusion.id = BloonString(fusands);
                fusion.baseId = fusion.id;
                return this;
            }

            public BloonsionReactor MergeProperties()
            {
                fusion.bloonProperties = fusands.Select(f => f.bloonProperties).Aggregate((a, b) => a | b);

                fusion.isBoss = fusands.Any(f => f.isBoss);
                fusion.isCamo = fusands.Any(f => f.isCamo);
                fusion.isFortified = fusands.Any(f => f.isFortified);
                fusion.isGrow = fusands.Any(f => f.isGrow);
                fusion.isInvulnerable = fusands.Any(f => f.isInvulnerable);
                fusion.isMoab = fusands.Any(f => f.isMoab);

                fusion.distributeDamageToChildren = fusands.Any(f => f.distributeDamageToChildren);
                fusion.tags = fusands.SelectMany(f => f.tags).Distinct().ToArray();

                return this;
            }

            public BloonsionReactor MergeHealth()
            {
                fusion.maxHealth = fusands.Select(f => f.maxHealth).Max();
                fusion.leakDamage = fusands.Select(f => f.leakDamage).Max();
                fusion.totalLeakDamage = fusands.Select(f => f.totalLeakDamage).Max();
                fusion.loseOnLeak = fusands.Any(f => f.loseOnLeak);
                return this;
            }

            public BloonsionReactor MergeSpeed()
            {
                fusion.speed = fusands.Select(f => f.speed).Max();
                fusion.speedFrames = fusands.Select(f => f.speed).Max();
                return this;
            }

            public BloonsionReactor MergeChildren()
            {
                var children = fusands.Select(f => new Bloonomial(f.GetBehavior<SpawnChildrenModel>().children))
                    .Aggregate((a, b) => a.Product(b)).terms.SelectMany(p => Fuse(p.Key, p.Value));
                fusion.GetBehavior<SpawnChildrenModel>().children = children.Select(c => c.id).ToArray();
                return this;
            }

            public BloonsionReactor MergeDisplay()
            {
                fusion.radius = fusands.Max(f => f.radius);
                //TODO: this lol
                //  rotate
                //  display
                //  etc
                //TODO: canonical weighting of base bloons to determine priority for visuals
                return this;
            }

            public BloonsionReactor MergeBehaviors()
            {
                //TODO: maybe this
                //  behaviors
                //  childDependents
                return this;
            }
        }
        public static IEnumerable<BloonModel> Fuse(IEnumerable<string> bloons, int count = 1)
        {
            return Fuse(bloons.Select(b => lookup[b]), count);
        }
        public static IEnumerable<BloonModel> Fuse(IEnumerable<BloonModel> bloons, int count = 1)
        {
            if (bloons.Count() == 0) return Enumerable.Empty<BloonModel>();
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
            return Enumerable.Repeat(bloon, count);
        }

        public static BloonModel Clone(BloonModel bloon)
        {
            return bloon.Clone().Cast<BloonModel>();
        }

        public static BloonModel Register(BloonModel bloon)
        {
            var game = Game.instance.model;
            MelonLogger.Msg("Creating " + bloon.id + " with children: " + string.Join(" ",bloon.GetBehavior<SpawnChildrenModel>().children));
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

        public static BloonGroupModel[] Split(RoundModel round, int[] sizes)
        {
            return Split(round, sizes, bloons => Fuse(bloons).First());
        }

        public static BloonGroupModel[] Split(RoundModel round, int[] sizes, Func<BloonModel[], BloonModel> fuser)
        {
            var groups = new List<BloonGroupModel>();
            var subgroups = new List<BloonGroupModel>();
            var bloons = new List<BloonModel>();
            var i = 0; var size = sizes[i];
            var j = 0; var group = round.groups[j];
            while (i < sizes.Length - 1 && j < round.groups.Length - 1)
            {
                bloons.Add(lookup[group.bloon]);
                var split = Split(group, size, out size);
                subgroups.Add(split.First());
                if (size > 0)
                {
                    group = round.groups[++j];
                    continue;
                }
                if (size == 0)
                {
                    group = split.Last();
                }
                else
                {
                    group = round.groups[++j];
                }
                size = sizes[++i];

                var bloon = Fuse(bloons).First();
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

        public static int RoundSize(RoundModel round)
        {
            return round.groups.Sum(g => g.count);
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
                    var size = RoundSize(rounds);
                    var parts = random.Next(1, size + 1);
                    MelonLogger.Msg("Splitting round " + (i++) + " of size " + size + " into " + parts + " parts!");
                    rounds.groups = Split(rounds, Partition(size, parts));
                    if (i > 30) break;
                }
            }
        }
    }
}
