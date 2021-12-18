using Assets.Scripts.Models;
using Assets.Scripts.Models.Bloons;
using Assets.Scripts.Models.Bloons.Behaviors;
using Assets.Scripts.Models.Effects;
using Assets.Scripts.Models.GenericBehaviors;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.UI_New.InGame;
using BTD_Mod_Helper.Extensions;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnhollowerRuntimeLib;
using UnhollowerBaseLib;
using static Combloonation.Main;
using static Combloonation.DisplaySystem;
using static Combloonation.Helpers;
using System.IO;
using UnityEngine;
using Assets.Scripts.Unity.UI_New.InGame.BloonMenu;

namespace Combloonation
{

    public static class Labloontory
    {

        public static readonly Dictionary<string, FusionBloonModel> _bloonsByName = new Dictionary<string, FusionBloonModel>();
        public static string fusionTag = "Fusion";
        public static string fusionComponentDelim = "ˇ";
        public static string fusionComponentDebuglim = "_";
        public static string fusionPropertiesDelim = "干";
        public static string fusionPropertiesDebuglim = "~";

        public static HashSet<string> unstackableBehaviors = new HashSet<string>
        {
            Il2CppType.Of<DisplayModel>().FullName,
        };
        public static HashSet<string> removeBehaviors = new HashSet<string>
        {
            Il2CppType.Of<SpawnBloonsActionModel>().FullName,
            Il2CppType.Of<SpawnChildrenModel>().FullName,
            Il2CppType.Of<DamageStateModel>().FullName,
            Il2CppType.Of<GrowModel>().FullName,
            Il2CppType.Of<SetGrowToOnChildrenModel>().FullName,
        };

        public struct Property
        {
            public static List<Property> all = new List<Property>
            {
                new Property( "Regrow", "Grow", true, b => b.isGrow, m => m.regen, b => {
                    b.isGrow = true;
                    b.tags = b.tags.Append("Grow").ToArray();
                    var rate = 3;
                    b.AddBehavior(new GrowModel("GrowModel_", rate, ""));
                    foreach (var child in b.childBloonModels) {
                        child.RemoveBehaviors<GrowModel>();
                        child.AddBehavior(new GrowModel("GrowModel_", rate, b.name));
                    }
                }),
                new Property( "Fortified", "Fortified", false, b => b.isFortified, m => m.fortified, b => {
                    b.isFortified = true;
                    b.tags = b.tags.Append("Fortified").ToArray();
                    b.maxHealth *= 2;
                }),
                new Property( "Camo", "Camo", true, b => b.isCamo, m => m.camo, b => {
                    b.isCamo = true;
                    b.tags = b.tags.Append("Camo").ToArray();
                }),
            };

            public readonly string name;
            public readonly string tag;
            public readonly bool heir;
            public readonly Func<BloonModel, bool> has;
            public readonly Func<BloonMenu, bool> menu;
            public readonly Action<FusionBloonModel> add;
            public Property(string name, string tag, bool heir, Func<BloonModel, bool> has, Func<BloonMenu, bool> menu, Action<FusionBloonModel> add)
            {
                this.name = name; this.tag = tag; this.heir = heir; this.has = has; this.menu = menu; this.add = add;
            }
        }

        public class FusionBloonModel : BloonModel
        {
            public readonly BloonModel[] fusands;
            public readonly Property[] props;
            public readonly string inherit;

            public FusionBloonModel(BloonModel f, BloonModel[] fs, Property[] ps)
                : base(f.id, f.baseId, f.speed, f.radius, f.display, f.damageDisplayStates, f.icon, f.rotate,
                      f.behaviors, f.overlayClass, f.tags, f.mods, f.collisionGroup, f.danger, f.hasChildrenWithDifferentTotalHealths,
                      f.layerNumber, f.isCamo, f.isGrow, f.isFortified, f.depletionEffects, f.rotateToFollowPath, f.isMoab,
                      f.isBoss, f.bloonProperties, f.leakDamage, f.maxHealth, f.distributeDamageToChildren, f.isInvulnerable,
                      f.propertyDisplays, f.bonusDamagePerHit, f.disallowCosmetics, f.isSaved, f.loseOnLeak)
            { fusands = fs; props = ps; inherit = PropertyString(ps.Where(p => p.heir)); }
        }

        public class BloonsionReactor
        {
            public readonly FusionBloonModel fusion;

            public BloonsionReactor(IEnumerable<BloonModel> bloons, IEnumerable<Property> props = null)
            {
                var components = BloonsFromBloons(bloons);
                var allProps = (props != null ? props : GetProperties(components)).ToList();
                var baseFusands = BaseBloonsFromBloons(components).OrderByDescending(f => f.danger).TakeAtMost(5);
                var name = BloonNameFromBloons(baseFusands.Select(f => f.name), allProps);
                var fusands = baseFusands.Select(b => BloonFromName(b.name + PropertyString(ProbeProperties(b, allProps))));
                fusion = new FusionBloonModel(fusands.First(), fusands.ToArray(), allProps.ToArray());
                fusion._name = fusion.name = fusion.id = name;
                fusion.baseId = BaseBloonNameFromName(fusion.name);
            }

            public BloonsionReactor Merge()
            {
                //MelonLogger.Msg("Creating " + DebugString(fusion.name));
                return MergeBehaviors().MergeDisplay().MergeChildren().MergeSpawnBloonsActionModel().MergeProperties().MergeStats();
            }

            public BloonsionReactor MergeDisplay()
            {
                fusion.overlayClass = fusion.baseId;
                fusion.damageDisplayStates = new DamageStateModel[] { };
                fusion.depletionEffects = new Il2CppReferenceArray<EffectModel>(fusion.fusands.SelectMany(f => f.depletionEffects).ToArray());
                fusion.propertyDisplays = new Il2CppStringArray(fusion.fusands.SelectMany(f => f.propertyDisplays ?? new Il2CppStringArray(new string[]{ })).ToArray());

                var prefix = $"{folderPath}/{DebugString(fusion.name)}";
                var texturePath = prefix + ".texture.png";
                var iconPath = prefix + ".icon.png";
                if (File.Exists(texturePath)) computedTextures[fusion.name] = LoadTexture(texturePath);
                if (File.Exists(iconPath)) {
                    computedIcons[fusion.name] = LoadTexture(iconPath);
                    fusion.SetHelpfulAdditionsBloon();
                }

                return this;
            }

            public BloonsionReactor MergeProperties()
            {
                fusion.isBoss = fusion.fusands.Any(f => f.isBoss);
                fusion.isCamo = fusion.fusands.Any(f => f.isCamo);
                fusion.isFortified = fusion.fusands.Any(f => f.isFortified);
                fusion.isGrow = fusion.fusands.Any(f => f.isGrow);
                fusion.isMoab = fusion.fusands.Any(f => f.isMoab);
                
                fusion.tags = fusion.fusands.SelectMany(f => f.tags).Append(fusionTag).Distinct().ToArray();
                fusion.bloonProperties = fusion.fusands.Select(f => f.bloonProperties).Aggregate((a, b) => a | b);
                foreach (var child in fusion.childBloonModels)
                    fusion.AddBehavior(new SetGrowToOnChildrenModel("SetGrowToOnChildrenModel_", child.baseId, fusion.baseId));
                if (fusion.isGrow) 
                {
                    var grows = fusion.fusands.SelectMany(f => f.GetBehaviors<GrowModel>());
                    var rate = Math.Max(3,grows.Max(g => g.rate));
                    fusion.AddBehavior(new GrowModel("GrowModel_", rate, ""));
                    foreach (var child in fusion.childBloonModels) {
                        var cgrow = child.GetBehaviors<GrowModel>().FirstOrDefault(g => g.growToId != "");
                        GrowModel grow = new GrowModel("GrowModel_", rate, fusion.name);
                        if (cgrow != default) grow.growToId = grow.growToId;
                        child.RemoveBehaviors<GrowModel>();
                        child.AddBehavior(grow);
                    }
                }
                foreach (var p in fusion.props) if (!p.has(fusion)) p.add(fusion);
                return this;
            }

            public BloonsionReactor MergeStats()
            {
                fusion.maxHealth = fusion.fusands.Max(f => f.maxHealth) * fusion.fusands.Count();
                fusion.leakDamage = fusion.fusands.Max(f => f.leakDamage)*fusion.fusands.Count();
                fusion.isInvulnerable = fusion.fusands.Any(f => f.isInvulnerable);
                fusion.loseOnLeak = fusion.fusands.Any(f => f.loseOnLeak);
                fusion.speed = fusion.fusands.Max(f => f.speed);
                fusion.distributeDamageToChildren = fusion.fusands.All(f => f.distributeDamageToChildren);
                fusion.totalLeakDamage = fusion.leakDamage + fusion.childBloonModels.ToList().Sum(c => c.totalLeakDamage);
                if (fusion.isMoab && fusion.isGrow) fusion.maxHealth *= 1.25f;
                return this;
            }

            public BloonsionReactor MergeBehaviors()
            {
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
                var models = children.Terms().SelectMany(p => Enumerable.Repeat(Fuse(p.Key, fusion.inherit), p.Value)).ToList();

                fusion.childBloonModels = models.ToIl2CppList();
                fusion.UpdateChildBloonModels();

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
                var models = children.Terms().SelectMany(p => {
                    if (p.Key.Count == 0) return new List<Model> { };
                    var model = p.Key.First().Item1.Duplicate();
                    model.spawnCount = p.Value;
                    var bloon = Fuse(p.Key.Select(t => t.Item2), fusion.inherit);
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

        public static string PropertyString(IEnumerable<Property> props)
        {
            var s = "";
            foreach (var p in Property.all) if (props.Contains(p)) s += p.name;
            return s;
        }

        public static BloonModel Clone(BloonModel bloon)
        {
            return bloon.Clone().Cast<BloonModel>();
        }

        public static GameModel GetGameModel()
        {
            var model = InGame.instance?.bridge?.Model;
            if (model is null) model = Game.instance.model;
            return model;
        }

        public static BloonModel Fuse(IEnumerable<BloonModel> bloons, IEnumerable<Property> props = null)
        {
            if (bloons.Count() == 0) return null;
            BloonModel bloon = null;
            if (props is null) props = GetProperties(bloons);
            else props = GetProperties(bloons).Concat(props).Distinct();
            foreach (var propSet in props.ToList().Power()) {
                var reactor = new BloonsionReactor(bloons, propSet);
                bloon = reactor.fusion;
                var oldBloon = BloonFromName(reactor.fusion.name, false);
                if (oldBloon != null) bloon = oldBloon;
                else Register(reactor.Merge().fusion);
            }
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

        public static string BloonNameFromBloons(IEnumerable<string> bases, IEnumerable<Property> props)
        {
            var name = string.Join(fusionComponentDelim, bases);
            if (bases.Count() > 1) name += fusionPropertiesDelim;
            return name + PropertyString(props);
        }

        public static string BloonNameFromNames(IEnumerable<string> names)
        {
            var name = string.Join(fusionComponentDelim, names);
            if (names.Count() > 1) name += fusionPropertiesDelim + PropertyString(GetProperties(names.Select(n => BloonFromName(n))));
            return name;
        }

        public static string BaseBloonNameFromName(string name)
        {
            name = BloonNameFromNames(name.Split(fusionPropertiesDelim).First().Split(fusionComponentDelim));
            foreach (var p in Property.all) name = name.Replace(p.name, "");
            return name;
        }

        public static IEnumerable<Property> ProbeProperties(BloonModel bloon, List<Property> allProps = null)
        {
            var props = new List<Property>();
            var id = bloon.baseId;
            allProps = allProps ?? Property.all;
            foreach (var p in allProps.ToArray())
            {
                bloon = BloonFromName(id + p.name, false);
                if (bloon != null && !(bloon is FusionBloonModel)) props.Add(p);
            }
            return props;
        }

        public static IEnumerable<Property> GetExtraProperties(string name)
        {
            var props = new List<Property>();
            foreach (var p in Property.all) if (name.Contains(p.name)) props.Add(p);
            return props;
        }

        public static IEnumerable<Property> GetExtraProperties(BloonModel bloon)
        {
            return GetExtraProperties(bloon.name);
        }

        public static IEnumerable<Property> GetBaseProperties(BloonModel bloon)
        {
            return GetProperties(BloonFromName(bloon.baseId));
        }

        public static IEnumerable<Property> GetProperties(BloonModel bloon)
        {
            var props = new List<Property>();
            foreach (var p in Property.all) if (p.has(bloon)) props.Add(p);
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

        public static IEnumerable<Property> GetProperties(IEnumerable<BloonModel> bloons)
        {
            return bloons.SelectMany(b => GetProperties(b)).Distinct();
        }

        public static IEnumerable<Property> GetExtraProperties(IEnumerable<BloonModel> bloons)
        {
            return bloons.SelectMany(b => GetExtraProperties(b)).Distinct();
        }

        public static IEnumerable<Property> GetBaseProperties(IEnumerable<BloonModel> bloons)
        {
            return bloons.SelectMany(b => GetBaseProperties(b)).Distinct();
        }

        public static BloonModel Fuse(IEnumerable<string> bloons, string props = null)
        {
            return Fuse(bloons.Select(b => BloonFromName(b)), props != null ? GetExtraProperties(props) : null);
        }
    }
}
