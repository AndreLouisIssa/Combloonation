using System.Linq;
using Assets.Scripts.Models.Bloons;
using BTD_Mod_Helper.Extensions;
using System.Collections.Generic;
using Assets.Scripts.Models.Bloons.Behaviors;
using MelonLoader;
using System;
using Assets.Scripts.Unity;
using UnhollowerRuntimeLib;
using Assets.Scripts.Models;
using Assets.Scripts.Unity.UI_New.InGame;

namespace Combloonation
{

    public static class Labloontory
    {

        public static readonly Dictionary<string, FusionBloonModel> _bloonsByName = new Dictionary<string, FusionBloonModel>();
        public static string fusionTag = "CombloonationFusion";
        public static string delim = $"({fusionTag})";
        public static string debuglim = "_";
        public static List<string> properties = new List<string>
        {
            "Regrow", "Fortified", "Camo"
        };
        public static HashSet<string> stackableBehaviors = new HashSet<string>
        {
            Il2CppType.Of<PopEffectModel>().FullName,
            Il2CppType.Of<CreateSoundOnDamageBloonModel>().FullName,
            Il2CppType.Of<DistributeCashModel>().FullName,
        };
        public static HashSet<string> removeBehaviors = new HashSet<string>
        {
            Il2CppType.Of<SpawnBloonsActionModel>().FullName,
            Il2CppType.Of<SpawnChildrenModel>().FullName,
        };

        public class FusionBloonModel : BloonModel
        {
            public readonly BloonModel[] fusands;

            public FusionBloonModel(BloonModel fusion, BloonModel[] fusands) : base(fusion.id, fusion.baseId, fusion.speed, fusion.radius, fusion.display, fusion.damageDisplayStates, fusion.icon, fusion.rotate, fusion.behaviors, fusion.overlayClass, fusion.tags, fusion.mods, fusion.collisionGroup, fusion.danger, fusion.hasChildrenWithDifferentTotalHealths, fusion.layerNumber, fusion.isCamo, fusion.isGrow, fusion.isFortified, fusion.depletionEffects, fusion.rotateToFollowPath, fusion.isMoab, fusion.isBoss, fusion.bloonProperties, fusion.leakDamage, fusion.maxHealth, fusion.distributeDamageToChildren, fusion.isInvulnerable, fusion.propertyDisplays, fusion.bonusDamagePerHit, fusion.disallowCosmetics, fusion.isSaved, fusion.loseOnLeak)
            {
                this.fusands = fusands;
            }
        }

        public class BloonsionReactor
        {
            public readonly FusionBloonModel fusion;
            public bool real = false;

            public BloonsionReactor(IEnumerable<BloonModel> bloons)
            {
                var noDuplicates = bloons.SelectMany(b => GetBloonNamesFromName(b.name)).Distinct().Select(s => GetBloonByName(s));
                var consolidatedProperties = noDuplicates.GroupBy(b => b.baseId).Select(g => g.Key +
                    GetPropertyString(g.Select(b => GetPropertiesFromName(b.name)).Aggregate((a, b) => a.Union(b))));
                var i = 0;
                var fusands = consolidatedProperties.Select(s => GetBloonByName(s)).OrderByDescending(f => f.danger).TakeWhile(f => i++<5);
                fusion = new FusionBloonModel(fusands.First(), fusands.ToArray());
                fusion.baseId = fusion._name = fusion.name = fusion.id = BloonsToName(fusion.fusands);
            }

            public BloonsionReactor Merge()
            {
                MelonLogger.Msg("Creating " + DebugString(fusion.name));
                return MergeProperties().MergeStats().MergeBehaviors().MergeChildren().MergeSpawnBloonsActionModel();
            }

            public BloonsionReactor MergeProperties()
            {
                fusion.bloonProperties = fusion.fusands.Select(f => f.bloonProperties).Aggregate((a, b) => a | b);

                fusion.isBoss = fusion.fusands.Any(f => f.isBoss);
                fusion.isCamo = fusion.fusands.Any(f => f.isCamo);
                fusion.isFortified = fusion.fusands.Any(f => f.isFortified);
                fusion.isGrow = fusion.fusands.Any(f => f.isGrow);
                fusion.isMoab = fusion.fusands.Any(f => f.isMoab);

                fusion.distributeDamageToChildren = fusion.fusands.All(f => f.distributeDamageToChildren);
                fusion.tags = fusion.fusands.SelectMany(f => f.tags).Append(fusionTag).Distinct().ToArray();

                return this;
            }

            public BloonsionReactor MergeStats()
            {
                fusion.maxHealth = fusion.fusands.Sum(f => f.maxHealth);
                fusion.isInvulnerable = fusion.fusands.Any(f => f.isInvulnerable);
                fusion.totalLeakDamage = fusion.leakDamage = fusion.fusands.Sum(f => f.leakDamage);
                fusion.loseOnLeak = fusion.fusands.Any(f => f.loseOnLeak);
                fusion.speed = fusion.fusands.Max(f => f.speed);
                return this;
            }

            public BloonsionReactor MergeBehaviors()
            {
                fusion.RemoveBehaviors<DamageStateModel>();
                fusion.damageDisplayStates = new DamageStateModel[] { };
                fusion.behaviors = fusion.fusands.SelectMany(f => f.behaviors.ToList()).GroupBy(b => b.GetIl2CppType().FullName)
                    .SelectMany(g => removeBehaviors.Contains(g.Key) ? new List<Model> { } : stackableBehaviors.Contains(g.Key) ? g.ToList() : new List<Model> { g.First() }).ToIl2CppReferenceArray();
                return this;
            }

            public BloonsionReactor MergeChildren()
            {
                var _behaviors = fusion.fusands.Select(f => f.GetBehaviors<SpawnChildrenModel>());
                var _children = _behaviors.Select(l => l.SelectMany(b => b.children));
                var behavior = _behaviors.First(l => l.Count > 0).First().Duplicate();

                var bound = _children.Max(c => c.Count());
                var children = _children.Select(c => new Combinomial<string>(c)).Aggregate((a, b) => a.Product(b).Cull().BoundAbove(bound));
                var models = children.Terms().SelectMany(p => Enumerable.Repeat(Fuse(p.Key), p.Value));

                fusion.childBloonModels = models.ToIl2CppList();
                fusion.UpdateChildBloonModels();
                fusion.totalLeakDamage = fusion.leakDamage + models.Sum(c => c.totalLeakDamage);

                behavior.children = models.Select(c => c.name).ToArray();
                fusion.AddBehavior(behavior);
                return this;
            }

            public BloonsionReactor MergeSpawnBloonsActionModel()
            {
                var _behaviors = fusion.fusands.Select(f => f.GetBehaviors<SpawnBloonsActionModel>());
                var _children = _behaviors.Select(l => l.Select(m => new Tuple<Tuple<SpawnBloonsActionModel, string>, int>(new Tuple<SpawnBloonsActionModel, string>(m, m.bloonType), m.spawnCount)));

                var bound = _behaviors.Max(l => l.Count == 0 ? 0 : l.Max(m => m.spawnCount));
                var children = _children.Select(c => new Ordinomial<Tuple<SpawnBloonsActionModel, string>>(c)).Aggregate((a, b) => a.Product(b).Cull().BoundAbove(bound));
                var models = children.Terms().SelectMany(p =>
                {
                    if (p.Key.Count == 0) return new List<Model> { };
                    var model = p.Key.First().Item1.Duplicate();
                    model.spawnCount = p.Value;
                    var bloon = Fuse(p.Key.Select(t => t.Item2));
                    model.bloonType = bloon.name;
                    return new List<Model> { model };
                });

                fusion.behaviors = fusion.behaviors.Concat(models).ToIl2CppReferenceArray();
                return this;
            }
        }

        public static string DebugString(string s)
        {
            return s.Replace(delim, debuglim);
        }
        public static Dictionary<int, string> Decompose(this string body, string[] parts)
        {
            var map = new Dictionary<int, string>();
            foreach (var part in parts)
            {
                var pos = body.IndexOf(part);
                if (pos < 0) continue;
                map[pos] = part;
                body = body.Remove(pos, part.Length);
            }
            return map;
        }
        public static string[] Split(this string s, string d)
        {
            if (s == "") return new string[] { };
            if (d == "") return s.Select(c => c.ToString()).ToArray();
            int i = s.IndexOf(d);
            var b = new List<string>();
            while (i >= 0)
            {
                b.Add(s.Substring(0, i));
                s = s.Substring(i + d.Length);
                i = s.IndexOf(d);
            }
            b.Add(s);
            return b.ToArray();
        }
        public static string BloonsToName(IEnumerable<BloonModel> bloons)
        {
            return string.Join(delim, bloons.Select(f => f.name));
        }

        public static IEnumerable<string> BloonsToNames(IEnumerable<BloonModel> bloons)
        {
            return bloons.Select(f => f.name);
        }

        public static IEnumerable<BloonModel> BloonsFromName(string name)
        {
            return GetBloonNamesFromName(name).Select(s => GetBloonByName(s));
        }

        public static IEnumerable<string> GetBloonNamesFromName(string name)
        {
            return name.Split(delim).Distinct();
        }
        public static IEnumerable<string> BaseBloonNamesFromName(string name)
        {
            foreach (var p in properties)
            {
                name = name.Replace(p, "");
            }
            return name.Split(delim).Distinct();
        }

        public static IEnumerable<string> GetPropertiesFromName(string name)
        {
            var props = new List<string>();
            foreach (var p in properties)
            {
                if (name.Contains(p)) props.Add(p);
            }
            return props;
        }

        public static string GetPropertyString(HashSet<string> props)
        {
            var s = "";
            foreach (var p in properties)
            {
                if (props.Contains(p)) s += p;
            }
            return s;
        }

        public static string GetPropertyString(IEnumerable<string> props)
        {
            return GetPropertyString(new HashSet<string>(props));
        }
        public static BloonModel Fuse(IEnumerable<string> bloons)
        {
            return Fuse(bloons.Select(b => GetBloonByName(b)));
        }
        public static BloonModel Fuse(IEnumerable<BloonModel> bloons)
        {
            if (bloons.Count() == 0) return null;
            var reactor = new BloonsionReactor(bloons);
            var bloon = (BloonModel)reactor.fusion;
            var oldBloon = GetBloonByName(bloon.name, false);
            if (oldBloon != null) bloon = oldBloon;
            else Register(reactor.Merge().fusion);
            return bloon;
        }

        public static BloonModel Clone(BloonModel bloon)
        {
            return bloon.Clone().Cast<BloonModel>();
        }

        public static GameModel GetGameModel()
        {
            var model = InGame.instance?.bridge?.Model;
            if (model == null) model = Game.instance.model;
            return model;
        }

        public static BloonModel GetBloonByName(string name, bool direct = true)
        {
            var exists = _bloonsByName.TryGetValue(name, out var model);
            if (exists) return model;
            var lookup = GetGameModel().bloonsByName;
            if (direct || lookup.ContainsKey(name)) return lookup[name];
            return null;
        }

        public static BloonModel Register(FusionBloonModel bloon, bool inGame = false)
        {
            _bloonsByName[bloon.name] = bloon;
            var model = GetGameModel();
            if (!model.bloons.Contains(bloon)) model.bloons = model.bloons.Prepend(bloon).ToArray();
            model.bloonsByName[bloon.name] = bloon;
            //MelonLogger.Msg("Registered " + DebugString(bloon.name));
            return bloon;
        }
    }
}
