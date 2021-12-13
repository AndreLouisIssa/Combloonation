using Assets.Scripts.Models;
using Assets.Scripts.Models.Bloons;
using Assets.Scripts.Models.Bloons.Behaviors;
using Assets.Scripts.Models.GenericBehaviors;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.UI_New.InGame;
using BTD_Mod_Helper.Extensions;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnhollowerRuntimeLib;

namespace Combloonation
{

    public static class Labloontory
    {

        public static readonly Dictionary<string, FusionBloonModel> _bloonsByName = new Dictionary<string, FusionBloonModel>();
        public static string fusionComponentTag = "CombloonationFusionComponent";
        public static string fusionComponentDelim = $"({fusionComponentTag})";
        public static string fusionComponentDebuglim = "_";
        public static string fusionPropertiesTag = "CombloonationFusionProperties";
        public static string fusionPropertiesDelim = $"({fusionPropertiesTag})";
        public static string fusionPropertiesDebuglim = "~";
        public static List<string> properties = new List<string>
        {
            "Regrow", "Fortified", "Camo"
        };
        public static HashSet<string> unstackableBehaviors = new HashSet<string>
        {
            Il2CppType.Of<DisplayModel>().FullName,
        };
        public static HashSet<string> removeBehaviors = new HashSet<string>
        {
            Il2CppType.Of<SpawnBloonsActionModel>().FullName,
            Il2CppType.Of<SpawnChildrenModel>().FullName,
            Il2CppType.Of<DamageStateModel>().FullName,
        };

        public class FusionBloonModel : BloonModel
        {
            public readonly BloonModel[] fusands;

            public FusionBloonModel(BloonModel f, BloonModel[] fs)
                : base(f.id, f.baseId, f.speed, f.radius, f.display, f.damageDisplayStates, f.icon, f.rotate,
                      f.behaviors, f.overlayClass, f.tags, f.mods, f.collisionGroup, f.danger, f.hasChildrenWithDifferentTotalHealths,
                      f.layerNumber, f.isCamo, f.isGrow, f.isFortified, f.depletionEffects, f.rotateToFollowPath, f.isMoab,
                      f.isBoss, f.bloonProperties, f.leakDamage, f.maxHealth, f.distributeDamageToChildren, f.isInvulnerable,
                      f.propertyDisplays, f.bonusDamagePerHit, f.disallowCosmetics, f.isSaved, f.loseOnLeak)
            { fusands = fs; }
        }

        public class BloonsionReactor
        {
            public readonly FusionBloonModel fusion;

            public BloonsionReactor(IEnumerable<BloonModel> bloons)
            {
                var components = BloonsFromBloons(bloons);
                var allProps = GetProperties(components).ToList();
                var baseFusands = BaseBloonsFromBloons(components).OrderByDescending(f => f.name).OrderByDescending(f => f.danger).TakeAtMost(5);
                var name = BloonNameFromBloons(baseFusands.Select(f => f.name), allProps);
                var fusands = baseFusands.Select(b => BloonFromName(b.name + GetPropertyString(ProbeProperties(b, allProps))));
                fusion = new FusionBloonModel(fusands.First(), fusands.ToArray());
                fusion._name = fusion.name = fusion.id = name;
                fusion.baseId = BaseBloonNameFromName(fusion.name);
            }

            public BloonsionReactor Merge()
            {
                if (fusion.name != fusion.baseId) Fuse(BloonNamesFromName(fusion.baseId));
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
                fusion.tags = fusion.fusands.SelectMany(f => f.tags).Append(fusionComponentTag).Distinct().ToArray();

                return this;
            }

            public BloonsionReactor MergeStats()
            {
                fusion.maxHealth = fusion.fusands.Max(f => f.maxHealth) * fusion.fusands.Count();
                fusion.totalLeakDamage = fusion.leakDamage = fusion.fusands.Max(f => f.leakDamage)*fusion.fusands.Count();
                fusion.isInvulnerable = fusion.fusands.Any(f => f.isInvulnerable);
                fusion.loseOnLeak = fusion.fusands.Any(f => f.loseOnLeak);
                fusion.speed = fusion.fusands.Max(f => f.speed);
                return this;
            }

            public BloonsionReactor MergeBehaviors()
            {
                fusion.damageDisplayStates = new DamageStateModel[] { };
                fusion.behaviors = fusion.fusands.SelectMany(f => f.behaviors.ToList()).GroupBy(b => b.GetIl2CppType().FullName)
                    .SelectMany(g => removeBehaviors.Contains(g.Key) ? new List<Model> { } : !unstackableBehaviors.Contains(g.Key) ? g.ToList() : new List<Model> { g.First() }).ToIl2CppReferenceArray();
                fusion.childDependants = fusion.fusands.SelectMany(f => f.childDependants.ToList()).ToIl2CppList();
                return this;
            }

            public BloonsionReactor MergeChildren()
            {
                var _behaviors = fusion.fusands.Select(f => f.GetBehaviors<SpawnChildrenModel>());
                var _children = _behaviors.Select(l => l.SelectMany(b => b.children));
                if (_behaviors.Count() == 0 || _behaviors.All(l => l.Count == 0)) return this;
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
            return s.Replace(fusionComponentDelim, fusionComponentDebuglim).Replace(fusionPropertiesDelim, fusionPropertiesDebuglim);
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

        public static string GetPropertyString(IEnumerable<string> props)
        {
            var s = "";
            foreach (var p in properties) if (props.Contains(p)) s += p;
            return s;
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

        public static BloonModel Fuse(IEnumerable<BloonModel> bloons)
        {
            if (bloons.Count() == 0) return null;
            var reactor = new BloonsionReactor(bloons);
            var bloon = (BloonModel)reactor.fusion;
            var oldBloon = BloonFromName(bloon.name, false);
            if (oldBloon != null) bloon = oldBloon;
            else Register(reactor.Merge().fusion);
            return bloon;
        }

        public static BloonModel Register(FusionBloonModel bloon)
        {
            _bloonsByName[bloon.name] = bloon;
            var model = GetGameModel();
            if (!model.bloons.Contains(bloon)) model.bloons = model.bloons.Prepend(bloon).ToArray();
            model.bloonsByName[bloon.name] = bloon;
            model.AddChildDependant(bloon.Cast<BloonModel>());
            //MelonLogger.Msg("Registered " + DebugString(bloon.name));
            return bloon;
        }

        public static BloonModel BloonFromName(string name, bool direct = true)
        {
            var exists = _bloonsByName.TryGetValue(name, out var model);
            if (exists) return model;
            var lookup = GetGameModel().bloonsByName;
            if (direct || lookup.ContainsKey(name)) return lookup[name];
            return null;
        }

        public static string BloonNameFromBloons(IEnumerable<string> bases, IEnumerable<string> props)
        {
            var name = string.Join(fusionComponentDelim, bases);
            if (bases.Count() > 1) name += fusionPropertiesDelim + GetPropertyString(props);
            return name;
        }

        public static string BloonNameFromNames(IEnumerable<string> names)
        {
            var name = string.Join(fusionComponentDelim, names);
            if (names.Count() > 1) name += fusionPropertiesDelim + GetPropertyString(GetProperties(names.Select(n => BloonFromName(n))));
            return name;
        }

        public static string BaseBloonNameFromName(string name)
        {
            name = BloonNameFromNames(name.Split(fusionPropertiesDelim).First().Split(fusionComponentDelim));
            foreach (var p in properties) name = name.Replace(p, "");
            return name;
        }

        public static IEnumerable<string> ProbeProperties(BloonModel bloon, List<string> allProps = null)
        {
            var props = new List<string>();
            var id = bloon.baseId;
            var reduce = allProps != null;
            allProps = allProps ?? properties;
            foreach (var p in allProps.ToArray())
            {
                if (BloonFromName(id + p, false) != null) {
                    props.Add(p);
                    if (reduce) allProps.Remove(p);
                }
            }
            return props;
        }

        public static IEnumerable<string> GetExtraProperties(BloonModel bloon)
        {
            var name = bloon.name;
            var props = new List<string>();
            foreach (var p in properties) if (name.Contains(p)) props.Add(p);
            return props;
        }

        public static IEnumerable<string> GetBaseProperties(BloonModel bloon)
        {
            return GetProperties(BloonFromName(bloon.baseId));
        }

        public static IEnumerable<string> GetProperties(BloonModel bloon)
        {
            var props = new List<string>();
            if (bloon.isGrow) props.Add("Regrow");
            if (bloon.isFortified) props.Add("Fortified");
            if (bloon.isCamo) props.Add("Camo");
            return props;
        }

        public static IEnumerable<string> BloonNamesFromName(string name)
        {
            return name.Split(fusionPropertiesDelim).First().Split(fusionComponentDelim).Distinct();
        }

        public static IEnumerable<string> BloonNamesFromBloons(IEnumerable<BloonModel> bloons)
        {
            return bloons.SelectMany(f => BloonNamesFromName(f.name)).Distinct();
        }

        public static IEnumerable<BloonModel> BloonsFromBloons(IEnumerable<BloonModel> bloons)
        {
            return BloonNamesFromBloons(bloons).Select(s => BloonFromName(s));
        }

        public static IEnumerable<string> BaseBloonNamesFromName(string name)
        {
            return BloonNamesFromName(BaseBloonNameFromName(name));
        }

        public static IEnumerable<string> BaseBloonNamesFromNames(IEnumerable<string> names)
        {
            return names.SelectMany(n => BaseBloonNamesFromName(n)).Distinct();
        }

        public static IEnumerable<string> BaseBloonNamesFromBloons(IEnumerable<BloonModel> bloons)
        {
            return BaseBloonNamesFromNames(BloonNamesFromBloons(bloons));
        }

        public static IEnumerable<BloonModel> BaseBloonsFromBloons(IEnumerable<BloonModel> bloons)
        {
            return BaseBloonNamesFromBloons(bloons).Select(n => BloonFromName(n));
        }

        public static IEnumerable<string> GetProperties(IEnumerable<BloonModel> bloons)
        {
            return bloons.SelectMany(b => GetProperties(b)).Distinct();
        }

        public static IEnumerable<string> GetExtraProperties(IEnumerable<BloonModel> bloons)
        {
            return bloons.SelectMany(b => GetExtraProperties(b)).Distinct();
        }

        public static IEnumerable<string> GetBaseProperties(IEnumerable<BloonModel> bloons)
        {
            return bloons.SelectMany(b => GetBaseProperties(b)).Distinct();
        }

        public static BloonModel Fuse(IEnumerable<string> bloons)
        {
            return Fuse(bloons.Select(b => BloonFromName(b)));
        }


        public static BloonModel Fuse(params string[] bloons)
        {
            return Fuse(bloons);
        }

        public static BloonModel Fuse(params BloonModel[] bloons)
        {
            return Fuse(bloons);
        }
    }
}
