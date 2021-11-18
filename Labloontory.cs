using System.Linq;
using Assets.Scripts.Unity;
using Assets.Scripts.Models.Bloons;
using Assets.Scripts.Models.Rounds;
using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;
using Assets.Scripts.Models.Bloons.Behaviors;
using MelonLoader;

namespace Combloonation
{

    public static class Labloontory
    {

        public static string BloonString(IEnumerable<BloonModel> bloons, bool fusion = false)
        {
            if (fusion) return $"Fusion~{BloonString(bloons)}";
            return string.Join("_", bloons.Select(f => f.id));
        }
        public class Bloonomial {

            public readonly Dictionary<HashSet<BloonModel>, int> terms = new Dictionary<HashSet<BloonModel>, int>(HashSet<BloonModel>.CreateSetComparer());

            public Bloonomial(IEnumerable<BloonModel> bloons = null)
            {
                if (bloons == null) return;
                terms[new HashSet<BloonModel> { }] = 1;
                foreach (BloonModel bloon in bloons)
                {
                    var k = new HashSet<BloonModel> { bloon };
                    terms.TryGetValue(k, out int d);
                    terms[k] = 1 + d;
                }
            }
            public Bloonomial Product(Bloonomial p, bool cull = true)
            {
                //polynomial product
                var r = new Bloonomial();
                foreach (var i in terms.Keys)
                {
                    foreach (var j in p.terms.Keys)
                    {
                        var k = new HashSet<BloonModel>(i.Concat(j));
                        r.terms.TryGetValue(k, out int d);
                        r.terms[k] = terms[i] * p.terms[j] + d;
                    }
                }
                if (cull)
                {
                    //cull lower order terms
                    var rem = new List<HashSet<BloonModel>>();
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

                fusands = new HashSet<BloonModel>(bloons);
                fusion = Clone(fusands.First());
            }

            public BloonsionReactor Merge()
            {
                return MergeId().MergeProperties().MergeHealth().MergeSpeed().MergeDisplay().MergeBehaviors().MergeChildren();
            }

            public BloonsionReactor MergeId()
            {
                fusion.id = BloonString(fusands, true);
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
                var children = fusands.Select(f => new Bloonomial(f.GetBehavior<SpawnChildrenModel>().children.Select(s => Game.instance.model.bloonsByName[s])))
                    .Aggregate((a, b) => a.Product(b)).terms.SelectMany(p => Fuse(p.Key, p.Value));
                fusion.GetBehavior<SpawnChildrenModel>().children = children.Select(c => c.id).ToArray();

                //fusion.updateChildBloonModels = true;
                //fusion.childBloonModels = new Il2CppSystem.Collections.Generic.List<BloonModel> { };
                //foreach (var child in children)
                //{
                //    fusion.childBloonModels.Add(child);
                //}
                //fusion.childBloonModels = fusion.childBloonModels;

                return this;
            }

            public BloonsionReactor MergeDisplay()
            {
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

        public static IEnumerable<BloonModel> Fuse(IEnumerable<BloonModel> bloons, int count = 1)
        {
            if (bloons.Count() == 0) return Enumerable.Empty<BloonModel>();
            var reactor = new BloonsionReactor(bloons).MergeId();
            var bloon = reactor.fusion;
            var lookup = Game.instance.model.bloonsByName;
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
            game.bloons = game.bloons.Prepend(bloon).ToArray();
            game.bloonsByName[bloon.id] = bloon;
            return bloon;
        }

        public static void MutateRounds()
        {
            //DEBUG TEST INSTANCE
            foreach (RoundSetModel round in Game.instance.model.roundSets)
            {
                var fusion = Fuse(round.rounds[35].groups.Select(g => Game.instance.model.bloonsByName[g.bloon])).First();
                foreach (var roundss in round.rounds)
                {
                    foreach (var group in roundss.groups)
                    {
                        group.bloon = fusion.id;
                        group.count = 1;
                        group.end = group.start;
                    }
                }
            }
        }
    }
}
