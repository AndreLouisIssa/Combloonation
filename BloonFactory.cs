using System.Linq;
using Assets.Scripts.Unity;
using Assets.Scripts.Models.Bloons;
using Assets.Scripts.Models.Rounds;
using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;

namespace Combloonation
{

    public static class BloonFactory
    {
        //TODO: statically cache BloonModel based on Id
        //  possibly by mutating Game.instance.model.bloonsByName
        //  maybe it already gets mutated by the setter of Game.instance.model.bloons

        public class BloonAdapter
        {
            public readonly IEnumerable<BloonModel> fusands;
            public readonly BloonModel fusion;

            public BloonAdapter(IEnumerable<BloonModel> bloons)
            {
                fusion = Clone(bloons.First());
                fusands = bloons.Distinct().OrderByDescending(f => f.baseId);
            }

            public BloonAdapter Merge()
            {
                return MergeId().MergeProperties().MergeHealth().MergeSpeed().MergeDisplay().MergeChildren();
            }

            public BloonAdapter MergeId()
            {
                fusion.id = $"Fusion~{string.Join("_", fusands.Select(f => f.id))}";
                fusion.baseId = fusion.id;
                return this;
            }

            public BloonAdapter MergeProperties()
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

            public BloonAdapter MergeHealth()
            {
                fusion.maxHealth = fusands.Select(f => f.maxHealth).Max();
                fusion.leakDamage = fusands.Select(f => f.leakDamage).Max();
                fusion.totalLeakDamage = fusands.Select(f => f.totalLeakDamage).Max();
                return this;
            }

            public BloonAdapter MergeSpeed()
            {
                fusion.speed = fusands.Select(f => f.speed).Max();
                fusion.speedFrames = fusands.Select(f => f.speed).Max();
                return this;
            }

            public BloonAdapter MergeChildren()
            {
                //TODO: this lol
                //  updateChildBloonModels
                //  childBloonModels
                //  childDependents
                //  GetComponent<SpawnChildrenModel>.children
                return this;
            }

            public BloonAdapter MergeDisplay()
            {
                //TODO: this lol
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
                var fusion = (new BloonAdapter(round.rounds[2].groups.Select(g => Game.instance.model.bloonsByName[g.bloon]))).Merge().fusion;
                Register(fusion);
                round.rounds[14].groups[0].bloon = fusion.id;
                round.rounds[14].groups[0].count = 1;
            }
        }
    }
}
