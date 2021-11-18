using System.Linq;
using Assets.Scripts.Unity;
using Assets.Scripts.Models.Bloons;
using Assets.Scripts.Models.Rounds;
using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;

namespace Combloonation
{

    public static class Bloonspawn
    {
        //TODO: statically cache BloonModel based on Id
        //  possibly by mutating Game.instance.model.bloonsByName
        //  maybe it already gets mutated by the setter of Game.instance.model.bloons

        //TODO: canonical weighting of base bloons to determine priority for visuals

        public class Bloonomial {

            public readonly Dictionary<HashSet<BloonModel>, int> terms = new Dictionary<HashSet<BloonModel>, int>(HashSet<BloonModel>.CreateSetComparer());

            public Bloonomial(IEnumerable<BloonModel> bloons = null)
            {
                terms[new HashSet<BloonModel> { }] = 1;
                if (bloons == null) return;
                foreach(BloonModel bloon in bloons)
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
                    int n = r.terms.Keys.Max(k => k.Count);
                    foreach (var k in r.terms.Keys)
                    {
                        if (k.Count < n)
                        {
                            r.terms.Remove(k);
                        }
                    }
                }
                return r;
            }
        }

        public class Bloonsion
        {
            public readonly IEnumerable<BloonModel> fusands;
            public readonly BloonModel fusion;

            public Bloonsion(IEnumerable<BloonModel> bloons)
            {

                fusands = bloons.Distinct().OrderBy(f => f.id);
                fusion = Clone(fusands.First());
            }

            public Bloonsion Merge()
            {
                return MergeId().MergeProperties().MergeHealth().MergeSpeed().MergeDisplay().MergeBehaviors().MergeChildren();
            }

            public Bloonsion MergeId()
            {
                fusion.id = $"Fusion~{string.Join("_", fusands.Select(f => f.id))}";
                fusion.baseId = fusion.id;
                return this;
            }

            public Bloonsion MergeProperties()
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

            public Bloonsion MergeHealth()
            {
                fusion.maxHealth = fusands.Select(f => f.maxHealth).Max();
                fusion.leakDamage = fusands.Select(f => f.leakDamage).Max();
                fusion.totalLeakDamage = fusands.Select(f => f.totalLeakDamage).Max();
                return this;
            }

            public Bloonsion MergeSpeed()
            {
                fusion.speed = fusands.Select(f => f.speed).Max();
                fusion.speedFrames = fusands.Select(f => f.speed).Max();
                return this;
            }

            public Bloonsion MergeChildren()
            {
                fusion.updateChildBloonModels = true;

                //TODO: this lol
                //  childBloonModels

                //  GetComponent<SpawnChildrenModel>.children
                return this;
            }

            public Bloonsion MergeDisplay()
            {
                //TODO: this lol
                //  rotate
                //  display
                //  etc
                return this;
            }

            public Bloonsion MergeBehaviors()
            {
                //TODO: maybe this
                //  behaviors
                //  childDependents
                return this;
            }
        }

        public static BloonModel Clone(BloonModel bloon)
        {
            return bloon.Clone().Cast<BloonModel>();
        }

        public static void Register(BloonModel bloon)
        {
            var game = Game.instance.model;
            game.bloons = game.bloons.Prepend(bloon).ToArray();
            //game.bloonsByName.Add(bloon.id, bloon);
        }

        public static void MutateRounds()
        {
            //DEBUG TEST INSTANCE
            foreach (RoundSetModel round in Game.instance.model.roundSets)
            {
                var fusion = (new Bloonsion(round.rounds[15].groups.Select(g => Game.instance.model.bloonsByName[g.bloon]))).Merge().fusion;
                Register(fusion);
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
