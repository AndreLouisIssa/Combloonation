using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Bloons;
using Il2CppAssets.Scripts.Models.Bloons.Behaviors;
using Il2CppAssets.Scripts.Models.GenericBehaviors;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.BloonMenu;
using Il2CppAssets.Scripts.Utils;
using Il2CppInterop.Runtime;

using System;
using System.Collections.Generic;

using static Combloonation.Display;
using static Combloonation.Helpers;
using static Combloonation.Main;

namespace Combloonation
{

    public static class Labloontory
    {

        public static readonly Dictionary<string, BloonModel> BloonsByName = [];
        private static readonly Dictionary<BloonModel, Fusion> fusionBloons = [];

        public static readonly string FusionTag = "Fusion";
        public static readonly string FusionComponentDelim = "ˇ";
        public static readonly string FusionComponentDebuglim = "'";
        public static readonly string FusionPropertiesDelim = "‑";
        public static readonly string FusionPropertiesDebuglim = "-";

        internal static HashSet<string> unstackableBehaviors =
        [
            Il2CppType.Of<DisplayModel>().FullName,
        ];
        internal static HashSet<string> removeBehaviors =
        [
            Il2CppType.Of<SpawnBloonsActionModel>().FullName,
            Il2CppType.Of<SpawnChildrenModel>().FullName,
            Il2CppType.Of<DamageStateModel>().FullName,
            Il2CppType.Of<GrowModel>().FullName,
            //Il2CppType.Of<SetGrowToOnChildrenModel>().FullName,
            Il2CppType.Of<DistributeCashModel>().FullName
        ];

        public readonly struct Property(string name, string tag, bool heir, Func<BloonModel, bool> has, Func<BloonMenu, bool> menu, Action<Labloontory.Fusion> add)
        {
            public static readonly List<Property> all =
            [
                new Property( "Regrow", "Grow", true, b => b.isGrow, m => m.regen, f => {
                    var b = f.bloon;
                    b.isGrow = true;
                    b.tags = b.tags.Append("Grow").ToArray();
                    var rate = 3;
                    b.AddBehavior(new GrowModel("GrowModel_", rate, null, null));
                    foreach (var child in b.childBloonModels) {
                        var grows = child.GetBehaviors<GrowModel>();
                        var cgrow = grows.Where(g => g.growToId != null).OrderBy(g => BloonFromNameSafe(g.growToId)?.danger ?? float.PositiveInfinity).FirstOrDefault();
                        var crate = Math.Max(rate, grows.Max(g => g.rate));
                        GrowModel grow = new("GrowModel_", crate, b.baseId + f.heir, null);
                        if (cgrow != default) grow.growToId = cgrow.growToId;
                        child.RemoveBehaviors<GrowModel>();
                        child.AddBehavior(grow);
                    }
                }),
                new Property( "Fortified", "Fortified", false, b => b.isFortified, m => m.fortified, f => {
                    var b = f.bloon;
                    b.isFortified = true;
                    b.tags = b.tags.Append("Fortified").ToArray();
                    b.maxHealth *= 2;
                }),
                new Property( "Camo", "Camo", true, b => b.isCamo, m => m.camo, f => {
                    var b = f.bloon;
                    b.isCamo = true;
                    b.tags = b.tags.Append("Camo").ToArray();
                }),
            ];

            public readonly string name = name;
            public readonly string tag = tag;
            public readonly bool heir = heir;
            public readonly Func<BloonModel, bool> has = has;
            public readonly Func<BloonMenu, bool> menu = menu;
            public readonly Action<Fusion> add = add;
        }

        private static Fusion NewFusion(this BloonModel bloon, BloonModel[] fusands, Property[] properties)
        {
            return new Fusion(bloon.Duplicate(), fusands, properties);
        }

        public static Fusion? GetFusion(this BloonModel bloon)
        {
            if (fusionBloons.TryGetValue(bloon, out var fusion)) return fusion;
            return null;
        }

        public class Fusion
        {
            public readonly BloonModel bloon;
            public readonly BloonModel[] fusands;
            public readonly Property[] properties;
            public readonly string heir;

            public Fusion(BloonModel bloon, BloonModel[] fusands, params Property[] properties)
            {
                this.bloon = bloon; this.fusands = fusands; this.properties = properties;

                this.heir = PropertyString(properties.Where(p => p.heir));

                fusionBloons[bloon] = this;
            }
        }

        public class BloonsionReactor
        {
            public readonly Fusion fusion;
            public readonly BloonModel bloon;
            public readonly BloonModel[] fusands;

            public static BloonModel React(IEnumerable<BloonModel> bloons, IEnumerable<Property>? props = null)
            {
                var baseFusands = BaseBloonsFromBloons(BloonsFromBloons(bloons)).OrderByDescending(f => f.danger).Take(maxFusands);
                var allProps = (props ?? GetProperties(baseFusands)).ToList();
                var name = BloonNameFromBloons(baseFusands.Select(f => f.name), allProps);
                var bloon = BloonFromNameSafe(name);
                if (bloon != null) return bloon;
                var reactor = new BloonsionReactor(baseFusands, allProps, name);
                Register(reactor.fusion.bloon);
                return reactor.Merge().fusion.bloon;
            }

            public BloonsionReactor(IEnumerable<BloonModel> baseFusands, List<Property> allProps, string name)
            {
                fusands = [.. baseFusands.Select(b => BloonFromName(b.name + PropertyString(ProbeProperties(b, allProps))))];
                var fusand = fusands.First() ?? throw new ArgumentException("There must be at least one fusand!", nameof(baseFusands));
                fusion = fusand.NewFusion([.. fusands], [.. allProps]);
                bloon = fusion.bloon;
                bloon._name = bloon.name = bloon.id = name;
                bloon.baseId = BaseBloonNameFromName(bloon.name);
            }

            public BloonsionReactor Merge()
            {
                //Log("Creating " + DebugString(fusion.name));
                return MergeBehaviors().MergeDisplay().MergeChildren().MergeSpawnBloonsActionModel().MergeProperties().MergeStats();
            }

            public BloonsionReactor MergeDisplay()
            {
                var bloon = fusion.bloon;
                //bloon.overlayClass = bloon.overlayClass;
                bloon.damageDisplayStates = Array.Empty<DamageStateModel>();
                bloon.depletionEffects = fusion.fusands.SelectMany(f => f.depletionEffects).ToIl2CppReferenceArray();
                //fusion.propertyDisplays = new Il2CppStringArray(fusion.fusands.SelectMany(f => f.propertyDisplays ?? new Il2CppStringArray(new string[]{ })).ToArray());

                //var prefix = $"{folderPath}/{DebugString(fusion.name)}";
                //var texturePath = prefix + ".texture.png";
                //var iconPath = prefix + ".icon.png";
                //if (File.Exists(texturePath)) computedTextures[fusion.name] = LoadTexture(texturePath);
                //if (File.Exists(iconPath)) {
                //    computedIcons[fusion.name] = LoadTexture(iconPath);
                //    fusion.SetHelpfulAdditionsBloon();
                //}

                return this;
            }

            public BloonsionReactor MergeProperties()
            {
                bloon.isBoss = fusands.Any(f => f.isBoss);
                bloon.isCamo = fusands.Any(f => f.isCamo);
                bloon.isFortified = fusands.Any(f => f.isFortified);
                bloon.isGrow = fusands.Any(f => f.isGrow);
                bloon.isMoab = fusands.Any(f => f.isMoab);

                bloon.tags = fusands.SelectMany(f => f.tags).Append(FusionTag).Distinct().ToArray();
                bloon.bloonProperties = fusands.Select(f => f.bloonProperties).Aggregate((a, b) => a | b);
                //foreach (var child in fusion.childBloonModels)
                //    fusion.AddBehavior(new SetGrowToOnChildrenModel("SetGrowToOnChildrenModel_", child.baseId, fusion.baseId));
                if (bloon.isGrow) 
                {
                    var grows = fusion.fusands.SelectMany(f => f.GetBehaviors<GrowModel>());
                    var rate = Math.Max(3,grows.Max(g => g.rate));
                    bloon.AddBehavior(new GrowModel("GrowModel_", rate, null, null));
                    foreach (var child in bloon.childBloonModels) {
                        grows = child.GetBehaviors<GrowModel>();
                        var cgrow = grows.Where(g => g.growToId != null).OrderBy(g => BloonFromNameSafe(g.growToId)?.danger ?? float.PositiveInfinity).FirstOrDefault();
                        var crate = Math.Max(rate, grows.Max(g => g.rate));
                        GrowModel grow = new("GrowModel_", crate, bloon.baseId + fusion.heir, null);
                        if (cgrow != default) grow.growToId = cgrow.growToId;
                        child.RemoveBehaviors<GrowModel>();
                        child.AddBehavior(grow);
                    }
                }
                foreach (var p in fusion.properties) if (!p.has(fusion.bloon)) p.add(fusion);
                return this;
            }

            public BloonsionReactor MergeStats()
            {
                var n = fusands.Length;

                bloon.maxHealth = fusands.Max(f => f.maxHealth) * n;
                bloon.leakDamage = fusands.Max(f => f.leakDamage) * n;
                bloon.leakDamageSet = fusands.Max(f => f.leakDamageSet) * n;
                bloon.isInvulnerable = fusands.Any(f => f.isInvulnerable);
                //bloon.loseOnLeak = fusands.Any(f => f.loseOnLeak);

                bloon.speed = fusands.Max(f => f.speed);
                bloon.distributeDamageToChildren = fusands.All(f => f.distributeDamageToChildren);
                //bloon.totalLeakDamage = new Il2CppSystem.Nullable<float>(bloon.leakDamage + bloon.childBloonModels.ToList().Sum(c => c.totalLeakDamage?.GetValueOrDefault(c.leakDamage) ?? 0));
                if (bloon.isMoab && bloon.isGrow) bloon.maxHealth = (int)(bloon.maxHealth * 1.25f);
                return this;
            }

            public BloonsionReactor MergeBehaviors()
            {
                bloon.behaviors = fusands.SelectMany(f => f.behaviors.ToList()).GroupBy(b => b.GetIl2CppType().FullName)
                    .SelectMany(g => removeBehaviors.Contains(g.Key) ? [] : !unstackableBehaviors.Contains(g.Key) ? g.ToList() : [g.First()]).ToIl2CppReferenceArray();
                bloon.childDependants = fusands.SelectMany(f => f.childDependants.ToList()).ToIl2CppList();

                var cashModels = fusands.SelectMany(f => f.GetBehaviors<DistributeCashModel>());
                var cash = cashModels.Sum(m => m.cash);
                var additive = cashModels.Sum(m => m.additive);
                var additionalCash = cashModels.Sum(m => m.additionalCash);
                var multiplier = cashModels.Select(m => m.multiplier).Aggregate((a, b) => a * b);
                var giveNoCash = cashModels.All(m => m.giveNoCash);
                bloon.behaviors = bloon.behaviors.AddTo(new DistributeCashModel("DistributeCashModel_", cash, additionalCash, multiplier, additive, giveNoCash));

                return this;
            }

            public BloonsionReactor MergeChildren()
            {
                var _behaviors = fusands.Select(f => f.GetBehaviors<SpawnChildrenModel>());
                var _children = _behaviors.Select(l => l.SelectMany(b => b.children));
                if (!_behaviors.Any() || _behaviors.All(l => l.Count == 0)) return this;
                var behavior = _behaviors.First(l => l.Count > 0).First().Duplicate();

                var bound = _children.Max(c => c.Count());
                var children = _children.Select(c => new Combinomial<string>(c)).Aggregate((a, b) => a.Product(b).Cull().BoundAbove(bound));
                var models = children.Terms().SelectMany(p => Enumerable.Repeat(Fuse(p.Key, fusion.heir), p.Value)).ToList();

                bloon.childBloonModels = models.ToIl2CppList();
                bloon.UpdateChildBloonModels();

                behavior.children = models.Select(c => c.name).ToArray();
                bloon.AddBehavior(behavior);
                return this;
            }

            private record struct Spawner(SpawnBloonsActionModel Spawn, string Name);

            public BloonsionReactor MergeSpawnBloonsActionModel()
            {
                var _behaviors = fusion.fusands.Select(f => f.GetBehaviors<SpawnBloonsActionModel>());
                var _children = _behaviors.Select(l => l.Select(m => new Ordinomial<Spawner>.Power(new Spawner(m, m.bloonType), m.spawnCount)));

                // find max spawn count
                var bound = 0;
                foreach (var l in _behaviors) foreach (var m in l)
                    if (bound < m.spawnCount) bound = m.spawnCount;

                var children = _children.Select(c => new Ordinomial<Spawner>(c)).Aggregate((a, b) => a.Product(b).Cull().BoundAbove(bound));

                var models = children.Terms().Where(p => p.Key.Count > 0).Select(p => {
                    var bloon = Fuse(p.Key.Select(t => t.Name), fusion.heir);
                    var model = p.Key.First().Spawn.Duplicate();
                    model.spawnCount = p.Value;
                    model.bloonType = bloon.name;
                    return model;
                });

                bloon.behaviors = bloon.behaviors.Concat(models).ToIl2CppReferenceArray();
                return this;
            }
        }

        public static string DebugString(string s)
        {
            return s.Replace(FusionComponentDelim, FusionComponentDebuglim).Replace(FusionPropertiesDelim, FusionPropertiesDebuglim);
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
            if (s == "") return [];
            if (d == "") return [.. s.Select(c => c.ToString())];
            int i = s.IndexOf(d);
            var b = new List<string>();
            while (i >= 0)
            {
                b.Add(s[..i]);
                s = s[(i + d.Length)..];
                i = s.IndexOf(d);
            }
            b.Add(s);
            return [.. b];
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

        public static BloonModel Fuse(IEnumerable<BloonModel> bloons, IEnumerable<Property>? props = null)
        {
            if (!bloons.Any()) throw new ArgumentException("Must have at least one fusand", nameof(bloons));
            BloonModel? bloon = null;
            if (props is null) props = GetProperties(bloons);
            else props = GetProperties(bloons).Concat(props).Distinct();
            foreach (var propSet in props.ToList().Power()) {
                bloon = BloonsionReactor.React(bloons, propSet);
            }
            if (bloon == null) throw new NullReferenceException($"{nameof(bloon)} is null!");
            return bloon;
        }

        public static BloonModel Register(BloonModel bloon)
        {
            BloonsByName[bloon.name] = bloon;
            
            var model = GetGameModel();
            if (!model.bloons.Contains(bloon))
            {
                model.bloons = model.bloons.Prepend(bloon).ToArray();
                if (model.bloonsByName != null) model.bloonsByName[bloon.name] = bloon; // TODO: populate it if it ever becomes non-null
                model.AddChildDependant(bloon);
                //Log("Registered NEW " + DebugString(bloon.name));
            }// else Log("Registered OLD " + DebugString(bloon.name));

            return bloon;
        }

        public static Fusion? FusionFromNameSafe(string name)
        {
            return BloonFromNameSafe(name)?.GetFusion();
        }

        public static BloonModel? BloonFromNameSafe(string name)
        {
            if (BloonsByName.TryGetValue(name, out var model) && model != null) return model;
            return null;
        }

        public static BloonModel BloonFromName(string name)
        {
            var exists = BloonsByName.TryGetValue(name, out var model);
            if (exists && model != null) return model;
            throw new KeyNotFoundException(name);
        }

        public static string BloonNameFromBloons(IEnumerable<string> bases, IEnumerable<Property> props)
        {
            var name = string.Join(FusionComponentDelim, bases);
            if (bases.Count() > 1) name += FusionPropertiesDelim;
            return name + PropertyString(props);
        }

        public static string BloonNameFromNames(IEnumerable<string> names)
        {
            var name = string.Join(FusionComponentDelim, names);
            if (names.Count() > 1) name += FusionPropertiesDelim + PropertyString(GetProperties(names.Select(n => BloonFromName(n))));
            return name;
        }

        public static string BaseBloonNameFromName(string name)
        {
            name = BloonNameFromNames(name.Split(FusionPropertiesDelim).First().Split(FusionComponentDelim));
            foreach (var p in Property.all) name = name.Replace(p.name, "");
            return name;
        }

        public static IEnumerable<Property> ProbeProperties(BloonModel bloon, List<Property>? allProps = null)
        {
            var props = new List<Property>();
            var id = bloon.baseId;
            allProps ??= Property.all;
            foreach (var p in allProps.ToArray())
            {
                var _bloon = BloonFromNameSafe(id + p.name);
                if (_bloon != null && _bloon.GetFusion() == null) props.Add(p);
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
            return name.Split(FusionPropertiesDelim).First().Split(FusionComponentDelim).Distinct();
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

        public static BloonModel Fuse(IEnumerable<string> bloons, string? props = null)
        {
            return Fuse(bloons.Select(b => BloonFromName(b)), props != null ? GetExtraProperties(props) : null);
        }
    }
}
