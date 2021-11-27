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
using UnhollowerRuntimeLib;
using Assets.Scripts.Models.GenericBehaviors;
using Assets.Scripts.Models;

namespace Combloonation
{

    public static class Labloontory
    {

        public static readonly Random random = new Random();
        public static Il2CppSystem.Collections.Generic.Dictionary<string, BloonModel> lookup;
        public static List<string> properties = new List<string>
        {
            "Regrow", "Fortified", "Camo"
        };

        public static string BloonsToId(IEnumerable<BloonModel> bloons)
        {
            return string.Join("_", bloons.Select(f => f.id));
        }

        public static IEnumerable<string> BloonsToIds(IEnumerable<BloonModel> bloons)
        {
            return  bloons.Select(f => f.id);
        }

        public static IEnumerable<BloonModel> BloonsFromId(string id)
        {
            return id.Split('_').Select(s => lookup[s]);
        }

        public static IEnumerable<string> BloonIdsFromId(string id)
        {
            return id.Split('_').Distinct();
        }
        public static IEnumerable<string> BaseBloonIdsFromId(string id)
        {
            foreach (var p in properties)
            {
                id = id.Replace(p, "");
            }
            return id.Split('_').Distinct();
        }

        public static IEnumerable<string> PropertiesFromId(string id)
        {
            var props = new List<string>();
            foreach (var p in properties)
            {
                if (id.Contains(p)) props.Add(p);
            }
            return props;
        }

        public static string PropertyString(HashSet<string> props)
        {
            var s = "";
            foreach (var p in properties)
            {
                if (props.Contains(p)) s += p;
            }
            return s;
        }

        public static string PropertyString(IEnumerable<string> props)
        {
            return PropertyString(new HashSet<string>(props));
        }

        public class BloonsionReactor
        {
            public readonly IEnumerable<BloonModel> fusands;
            public readonly BloonModel fusion;
            public bool real = false;

            public BloonsionReactor(IEnumerable<BloonModel> bloons)
            {
                var noDuplicates = bloons.SelectMany(b => BloonIdsFromId(b.id)).Distinct().Select(s => lookup[s]);
                var consolidatedProperties = noDuplicates.GroupBy(b => b.baseId).Select(g => g.First().baseId +
                    PropertyString(g.Select(b => PropertiesFromId(b.id)).Aggregate((a, b) => a.Union(b))));
                fusands = consolidatedProperties.Select(s => lookup[s]).OrderByDescending(f => f.danger);
                fusion = Clone(fusands.First());
                fusion.baseId = fusion._name = fusion.name = fusion.id = BloonsToId(fusands);
            }

            public BloonsionReactor Merge()
            {
                real = true;
                MelonLogger.Msg("Creating " + fusion.id + ":");
                return MergeProperties().MergeHealth().MergeSpeed().MergeDisplay().MergeBehaviors().MergeChildren();
            }

            public BloonsionReactor MergeProperties()
            {
                fusion.danger = fusands.Sum(f => f.danger);
                fusion.bloonProperties = fusands.Select(f => f.bloonProperties).Aggregate((a, b) => a | b);

                fusion.isBoss = fusands.Any(f => f.isBoss);
                fusion.isCamo = fusands.Any(f => f.isCamo);
                fusion.isFortified = fusands.Any(f => f.isFortified);
                fusion.isGrow = fusands.Any(f => f.isGrow);
                fusion.isMoab = fusands.Any(f => f.isMoab);

                fusion.distributeDamageToChildren = fusands.All(f => f.distributeDamageToChildren);
                fusion.tags = fusands.SelectMany(f => f.tags).Append("Fusion").Distinct().ToArray();
                if (real) MelonLogger.Msg("     - " + fusion.tags.Length + " tags");

                return this;
            }

            public BloonsionReactor MergeHealth()
            {
                fusion.maxHealth = fusands.Sum(f => f.maxHealth);
                fusion.isInvulnerable = fusands.Any(f => f.isInvulnerable);
                fusion.leakDamage = fusands.Sum(f => f.leakDamage);
                fusion.totalLeakDamage = fusands.Sum(f => f.totalLeakDamage);
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
                var behavior = fusion.GetBehavior<SpawnChildrenModel>();
                fusion.RemoveBehavior(behavior);
                behavior = behavior.Duplicate();
                var childModels = children.Terms().SelectMany(p => Enumerable.Repeat(Fuse(p.Key), p.Value));
                behavior.children = childModels.Select(c => c.id).ToArray();
                fusion.childBloonModels = childModels.ToIl2CppList();
                fusion.UpdateChildBloonModels();
                fusion.AddBehavior(behavior);
                return this;

            }

            public BloonsionReactor MergeDisplay()
            {
                fusion.radius = fusands.Max(f => f.radius);
                fusion.rotate = fusands.Any(f => f.rotate);
                fusion.rotateToFollowPath = fusands.Any(f => f.rotateToFollowPath);
                fusion.icon = fusands.First(f => f.icon != null).icon;
                fusion.RemoveBehaviors<DamageStateModel>();
                fusion.damageDisplayStates = new DamageStateModel[] { };
                return this;
            }

            public BloonsionReactor MergeBehaviors()
            {
                bool IsDisplayModel(Model model)
                {
                    return model.GetIl2CppType() == Il2CppType.Of<DisplayModel>();
                };
                //TODO: go through SpawnBloonsAction behaviors and merge the bloons between them
                fusion.behaviors = fusands.SelectMany(f => f.behaviors.ToList().Where(b => !IsDisplayModel(b))).Append(fusion.behaviors.First(b => IsDisplayModel(b))).ToIl2CppReferenceArray();
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
            var reactor = new BloonsionReactor(bloons);
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
            game.bloonsByName[bloon.name] = bloon;
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

        public static BloonGroupModel[] Split(BloonGroupModel[] roundGroups, int[] sizes, Func<List<BloonModel>, BloonModel> fuser)
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
