using System;
using System.Collections.Generic;
using System.Linq;
using static Combloonation.Labloontory;
using static Combloonation.Helpers;
using Assets.Scripts.Models.Rounds;
using Assets.Scripts.Models.Bloons;
using BTD_Mod_Helper.Extensions;
using MelonLoader;
using Assets.Scripts.Models;
using System.Runtime.Serialization;
using UnhollowerRuntimeLib;
using Bounds = Assets.Scripts.Models.Rounds.FreeplayBloonGroupModel.Bounds;
using static Combloonation.DirectableModel;
using UnhollowerBaseLib;
using HarmonyLib;

namespace Combloonation
{
    public class DirectableModel
    {
        public enum Directable
        {
            [EnumMember(Value = "GameModel")] GameModel,
            [EnumMember(Value = "RoundSetModel")] RoundSetModel,
            [EnumMember(Value = "RoundModel")] RoundModel,
            [EnumMember(Value = "BloonGroupModel")] BloonGroupModel,
            [EnumMember(Value = "BloonModel")] BloonModel,
            [EnumMember(Value = "FreeplayBloonGroupModel")] FreeplayBloonGroupModel,
            [EnumMember(Value = "BloonEmissionModel")] BloonEmissionModel
        }

        public static Dictionary<string, Directable> directableType = new Dictionary<string, Directable>
        {
            { Il2CppType.Of<GameModel>().FullName, Directable.GameModel },
            { Il2CppType.Of<RoundSetModel>().FullName, Directable.RoundSetModel },
            { Il2CppType.Of<RoundModel>().FullName, Directable.RoundModel },
            { Il2CppType.Of<BloonGroupModel>().FullName, Directable.BloonGroupModel },
            { Il2CppType.Of<BloonModel>().FullName, Directable.BloonModel },
            { Il2CppType.Of<FreeplayBloonGroupModel>().FullName, Directable.FreeplayBloonGroupModel },
            { Il2CppType.Of<BloonEmissionModel>().FullName, Directable.BloonEmissionModel }
        };

        public DirectableModel(GameModel model) { this.model = model; }
        public DirectableModel(RoundSetModel model) { this.model = model; }
        public DirectableModel(BloonGroupModel model) { this.model = model; }
        public DirectableModel(RoundModel model) { this.model = model; }
        public DirectableModel(BloonModel model) { this.model = model; }
        public DirectableModel(FreeplayBloonGroupModel model) { this.model = model; }
        public DirectableModel(BloonEmissionModel model) { this.model = model; }

        public static implicit operator DirectableModel(GameModel model) { return new DirectableModel(model); }
        public static implicit operator DirectableModel(RoundSetModel model) { return new DirectableModel(model); }
        public static implicit operator DirectableModel(BloonGroupModel model) { return new DirectableModel(model); }
        public static implicit operator DirectableModel(RoundModel model) { return new DirectableModel(model); }
        public static implicit operator DirectableModel(BloonModel model) { return new DirectableModel(model); }
        public static implicit operator DirectableModel(FreeplayBloonGroupModel model) { return new DirectableModel(model); }
        public static implicit operator DirectableModel(BloonEmissionModel model) { return new DirectableModel(model); }

        public static implicit operator GameModel(DirectableModel directable)
        {
            if (directable.model is GameModel model) return model;
            else throw new InvalidCastException($"The instance of {nameof(DirectableModel)} is not an instance of {nameof(GameModel)}");
        }
        public static implicit operator RoundSetModel(DirectableModel directable)
        {
            if (directable.model is RoundSetModel model) return model;
            else throw new InvalidCastException($"The instance of {nameof(DirectableModel)} is not an instance of {nameof(RoundSetModel)}");
        }
        public static implicit operator BloonGroupModel(DirectableModel directable)
        {
            if (directable.model is BloonGroupModel model) return model;
            else throw new InvalidCastException($"The instance of {nameof(DirectableModel)} is not an instance of {nameof(BloonGroupModel)}");
        }
        public static implicit operator RoundModel(DirectableModel directable)
        {
            if (directable.model is RoundModel model) return model;
            else throw new InvalidCastException($"The instance of {nameof(DirectableModel)} is not an instance of {nameof(RoundModel)}");
        }
        public static implicit operator BloonModel(DirectableModel directable)
        {
            if (directable.model is BloonModel model) return model;
            else throw new InvalidCastException($"The instance of {nameof(DirectableModel)} is not an instance of {nameof(BloonModel)}");
        }
        public static implicit operator FreeplayBloonGroupModel(DirectableModel directable)
        {
            if (directable.model is FreeplayBloonGroupModel model) return model;
            else throw new InvalidCastException($"The instance of {nameof(DirectableModel)} is not an instance of {nameof(FreeplayBloonGroupModel)}");
        }
        public static implicit operator BloonEmissionModel(DirectableModel directable)
        {
            if (directable.model is BloonEmissionModel model) return model;
            else throw new InvalidCastException($"The instance of {nameof(DirectableModel)} is not an instance of {nameof(BloonEmissionModel)}");
        }

        private readonly Model model;

        public static Directable GetDirectable<T>()
        {
            return directableType[Il2CppType.Of<T>().FullName];
        }

        public Directable GetDirectable()
        {
            return directableType[model.GetIl2CppType().FullName];
        }

        public bool Is<T>(out T outModel) where T : Model
        {
            return model.IsType(out outModel);
        }

        public T Cast<T>() where T : Model
        {
            model.IsType(out T outModel);
            return outModel;
        }

        public static implicit operator DirectableModel(Model model) { return new DirectableModel((dynamic)model); }
        public static implicit operator Model(DirectableModel directable) { return directable.model; }
    }

    public interface IDirector
    {
        float Eval(DirectableModel model);

        List<DirectableModel> Produce<M>(float? v = null, int? n = 1) where M : Model;

        List<DirectableModel> Produce(DirectableModel m, float? v = null, int? n = 1);
    }

    public abstract class SeededDirector : IDirector
    {
        public readonly Random random;
        public readonly int seed;

        public string GenName<T>()
        {
            return $"{typeof(T).Name}(@{GetType().Name})(#{(uint)random.NextDouble().GetHashCode()})";
        }

        public SeededDirector(int seed)
        {
            random = new Random(seed);
            this.seed = seed;
        }

        public SeededDirector() : this(new Random().Next()) { }

        public abstract float Eval(DirectableModel model);

        public abstract List<DirectableModel> Produce(DirectableModel m, float? v = null, int? n = 1);
        public abstract List<DirectableModel> Produce<M>(float? v = null, int? n = 1) where M : Model;
    }

    public class RoundMutatorDirector : SeededDirector
    {
        public RoundMutatorDirector(int seed) : base(seed) { }
        public RoundMutatorDirector() : base() { }

        public class GroupOnlyFreeplayBloonGroupModel : FreeplayBloonGroupModel
        {
            public GroupOnlyFreeplayBloonGroupModel(BloonGroupModel group) : base("", 0, new Bounds[] { }, group) { }
        }

        public static FreeplayBloonGroupModel[] Split(FreeplayBloonGroupModel fgroup, int size, out int excess)
        {
            var group = fgroup.group;
            var first = group.Duplicate();
            var span = group.count;
            excess = size - span;
            if (size <= 0 || size >= span)
                return new FreeplayBloonGroupModel[] { new FreeplayBloonGroupModel(fgroup.name, fgroup.score, fgroup.bounds, first) };
            var last = group.Duplicate();
            var step = size == 1 ? 0 : (group.end - group.start) / (span - 1);
            last.start = (first.end = group.start + size * step) + step;
            return new FreeplayBloonGroupModel[] { new FreeplayBloonGroupModel(fgroup.name, fgroup.score, fgroup.bounds, first), new FreeplayBloonGroupModel(fgroup.name, fgroup.score, fgroup.bounds, last) };
        }

        public static FreeplayBloonGroupModel[] Split(FreeplayBloonGroupModel[] inGroups, int[] sizes)
        {
            var fgroups = new List<FreeplayBloonGroupModel>();
            var subfgroups = new List<FreeplayBloonGroupModel>();
            var bloons = new List<string>();
            var i = 0; var size = sizes[i];
            var j = 0; var fgroup = inGroups[j];
            while (i < sizes.Length && j < inGroups.Length)
            {
                bloons.Add(fgroup.group.bloon);
                var split = Split(fgroup, size, out size);
                subfgroups.Add(split.First());
                if (size > 0 && ++j < inGroups.Length) { fgroup = inGroups[j]; continue; }
                if (size == 0) fgroup = split.Last();
                else if (++j < inGroups.Length) fgroup = inGroups[j];
                if (++i < sizes.Length) size = sizes[i];

                var bloon = Fuse(bloons);
                var bounds = subfgroups.SelectMany(f => f.bounds);
                var bound = (bounds.Count() > 0) ? NewFreeplayBounds(bounds.Min(b => b.lowerBounds), bounds.Max(b => b.upperBounds)) : NewFreeplayBounds(0, 0);
                foreach (var subgroup in subfgroups)
                {
                    subgroup.bounds = new Bounds[] { bound };
                    subgroup.group.bloon = bloon.name;
                    if (bloon is FusionBloonModel fusion)
                        subgroup.group.count = (int)Math.Ceiling(((double)subgroup.group.count) / fusion.fusands.Count());
                    fgroups.Add(subgroup);
                }
                bloons.Clear();
                subfgroups.Clear();
            }
            return fgroups.ToArray();
        }

        public static FreeplayBloonGroupModel[] Infuse(FreeplayBloonGroupModel[] inGroups, Random random)
        {
            var size = inGroups.Sum(g => g.group.count);
            var ratio = size/inGroups.Length;
            var parts = random.Next(Math.Min(size, ratio), Math.Max(size, ratio));
            return Split(inGroups, Partition(size, parts, random));
        }

        public static void Shift(FreeplayBloonGroupModel[] inGroups, int shift)
        {
            inGroups.Do(f => f.bounds = f.bounds.Select(b => NewFreeplayBounds(b.lowerBounds + shift, b.upperBounds + shift)).ToArray());
        }

        public static void Widen(FreeplayBloonGroupModel[] inGroups, int margin)
        {
            inGroups.Do(f => f.bounds = f.bounds.Select(b => NewFreeplayBounds(b.lowerBounds - margin, b.upperBounds + margin)).ToArray());
        }

        public static void Buff(FreeplayBloonGroupModel[] inGroups, float scale)
        {
            inGroups.Do(f => { var g = f.group = f.group.Duplicate(); g.count = (int)(g.count*scale); });
        }

        public static void Adjust(IEnumerable<FreeplayBloonGroupModel> inGroups)
        {
            inGroups.Do(f => { var g = f.group; g.end -= g.start; g.start = 0; });
        }

        public override float Eval(DirectableModel model) { return 0f; }

        public override List<DirectableModel> Produce(DirectableModel m, float? v = null, int? n = 1)
        {
            if (m.Is(out GameModel game))
            {
                foreach (var round in game.roundSets.SelectMany(r => r.rounds))
                {
                    var groups = round.groups; if (groups.Length <= 1) continue;
                    round.groups = Split(groups.Select(g => new GroupOnlyFreeplayBloonGroupModel(g)).ToArray(),
                        Partition(groups.Sum(g => g.count), random.Next(1, groups.Length), random)).Select(f => f.group).ToArray();
                }
                MelonLogger.Msg("Mutating rounds...");
                var roundGroups = new List<FreeplayBloonGroupModel> { };
                foreach (var rounds in game.roundSets.Select(r => r.rounds)) for (int i = 0; i < rounds.Length; ++i)
                {
                    var roundBounds = new Bounds[] { NewFreeplayBounds(i, i) };
                    rounds[i].groups.Do(g => roundGroups.Add(new FreeplayBloonGroupModel("FreeplayBloonGroupModel_", 0, roundBounds, g)));
                }
                //foreach (var roundSet in game.roundSets) roundSet.rounds = roundSet.rounds.Take(1).ToArray();
                roundGroups = roundGroups.ToArray().Iterate(l => Infuse(l, random)
                    .Apply(t => Shift(t, 100), t => Widen(t, 50), t => Buff(t, 2))).Take(3).SelectMany(s => s).Apply(Adjust).ToList();
                var bound = roundGroups.SelectMany(f => f.bounds).Max(b => b.upperBounds);
                game.freeplayGroups.Do(f => f.bounds = f.bounds.Where(b => b.upperBounds >= bound).ToArray());
                game.freeplayGroups.SelectMany(f => f.bounds).Do(b => b.lowerBounds = Math.Max(b.lowerBounds, bound));
                game.freeplayGroups = game.freeplayGroups.Concat(roundGroups).ToArray();
                //MelonLogger.Msg(string.Join("\n",game.freeplayGroups.OrderBy(f => f.CalculateScore(game)).Select(f => $"${f.CalculateScore(game)}: 
                //{f.group.count} x {f.group.bloon} ~> {f.group.end} | {string.Join(", ", f.bounds.Select(b => $"[{b.lowerBounds},{b.upperBounds}]"))}")));
                MelonLogger.Msg("Finished mutating rounds!");

                var list = new List<DirectableModel>(1);
                list.Add(game);
                return list;
            }
            throw new NotImplementedException();
        }

        public override List<DirectableModel> Produce<M>(float? v = null, int? n = 1) { throw new NotImplementedException(); }
    }

    public class RandomDirector : SeededDirector
    {
        public RandomDirector(int seed) : base(seed) { }

        public override float Eval(DirectableModel model) { return (float)random.NextDouble(); }

        public override List<DirectableModel> Produce(DirectableModel m, float? v = null, int? n = 1) { throw new NotImplementedException(); }
        public override List<DirectableModel> Produce<M>(float? v = null, int? _n = 1)
        {
            int n = _n ?? random.Next(1,20);
            var list = new List<DirectableModel> { };
            var game = GetGameModel();
            Action func = default;
            switch (GetDirectable<M>()) {
                case Directable.BloonModel:
                    func = () => {
                        var bloons = game.bloons;
                        var parts = random.Next(1, n);
                        var partition = Partition(n, parts, random);
                        var choice = bloons.Shuffle(random).Take(parts);
                        var i = 0;
                        list.AddItems(choice.SelectMany(b => Enumerable.Repeat(b, partition[i++])).Directable());
                    }; break;
                case Directable.BloonGroupModel:
                    func = () => {
                        var bloons = Produce<BloonModel>(v, n);
                        var groups = bloons.GroupWhile((a, b) => a == b).Select(c =>
                        {
                            var bloon = (BloonModel)c.First();
                            var start = (float)random.NextDouble() * random.Next(1,40);
                            var end = start + (float)random.NextDouble() * random.Next(1,40);
                            return new BloonGroupModel(GenName<BloonGroupModel>(), bloon.name, start, end, c.Count());
                        });
                        list.AddItems(groups.Directable());
                    }; break;
                case Directable.RoundModel:
                    func = () => {
                        var parts = random.Next(1, n);
                        var partition = Partition(n, parts, random);
                        var groupss = partition.Select(m => Produce<BloonGroupModel>(v, m));
                        var rounds = groupss.Select(gs => new RoundModel(GenName<RoundModel>(), gs.Cast<BloonGroupModel>().ToIl2CppReferenceArray()));
                        list.AddItems(rounds.Directable());
                    }; break;
                case Directable.RoundSetModel:
                    func = () => {
                        var parts = random.Next(1, n);
                        var partition = Partition(n, parts, random);
                        var roundss = partition.Select(m => Produce<RoundModel>(v, m));
                        var roundsets = roundss.Select(rs => new RoundSetModel(GenName<RoundSetModel>(), rs.Cast<RoundModel>().ToIl2CppReferenceArray()));
                        list.AddItems(roundsets.Directable());
                    }; break;
                case Directable.GameModel:
                    throw new NotImplementedException();
                case Directable.BloonEmissionModel:
                    throw new NotImplementedException();
                case Directable.FreeplayBloonGroupModel:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
            func();
            return list;
        }
    }

    public class TestDirector : SeededDirector
    {
        public TestDirector(int seed) : base(seed) { }

        public override float Eval(DirectableModel directable)
        {
            Func<float> func = default;
            switch (directable.GetDirectable())
            {
                case Directable.BloonModel:
                    func = () => { directable.Is(out BloonModel model);
                        return /*model.tags.Count + model.behaviors.Count + model.speed + model.maxHealth +*/ model.childBloonModels.ToList().GroupBy(b => b).Count() + model.childBloonModels.ToList().Sum(b => Eval(b));
                    }; break;
                case Directable.BloonGroupModel:
                    func = () => { directable.Is(out BloonGroupModel model);
                        return Eval(BloonFromName(model.bloon)) * model.count;// / (model.end - model.start);
                    }; break;
                case Directable.RoundModel:
                    func = () => { directable.Is(out RoundModel model);
                        return model.emissions.GroupBy(e => e.bloon).Count() + model.emissions.Sum(e => Eval(e)) + model.groups.GroupBy(g => g.bloon).Count() + model.groups.Sum(g => Eval(g));
                    }; break;
                case Directable.RoundSetModel:
                    func = () => { directable.Is(out RoundSetModel model);
                        return model.rounds.Sum(r => Eval(r));
                    }; break;
                case Directable.GameModel:
                    func = () => { directable.Is(out GameModel model);
                        return model.roundSets.Sum(r => Eval(r)) + model.freeplayGroups.Sum(g => Eval(g));
                    }; break;
                case Directable.BloonEmissionModel:
                    func = () => { directable.Is(out BloonEmissionModel model);
                        return Eval(BloonFromName(model.bloon));// / model.time;
                    }; break;
                case Directable.FreeplayBloonGroupModel:
                    func = () => { directable.Is(out FreeplayBloonGroupModel model);
                        return /*model.score + */ Eval(model.group) + model.bloonEmissions.GroupBy(e => e.bloon).Count() + model.bloonEmissions.Sum(e => Eval(e));
                    }; break;
                default:
                    throw new NotImplementedException();
            }
            return func();
        }

        public override List<DirectableModel> Produce(DirectableModel m, float? v = null, int? n = 1) { throw new NotImplementedException(); }
        public override List<DirectableModel> Produce<M>(float? v = null, int? n = 1) { throw new NotImplementedException(); }
    }

    public class DangerDirector : SeededDirector
    {
        public DangerDirector(int seed) : base(seed) { }

        public override float Eval(DirectableModel directable)
        {
            Func<float> func = default;
            switch (directable.GetDirectable())
            {
                case Directable.BloonModel:
                    func = () => { directable.Is(out BloonModel model);
                        return model.danger;
                    }; break;
                case Directable.BloonGroupModel:
                    func = () => { directable.Is(out BloonGroupModel model);
                        return Eval(BloonFromName(model.bloon)) * model.count;
                    }; break;
                case Directable.RoundModel:
                    func = () => { directable.Is(out RoundModel model);
                        return model.emissions.Sum(e => Eval(e)) + model.groups.Sum(g => Eval(g));
                    }; break;
                case Directable.RoundSetModel:
                    func = () => { directable.Is(out RoundSetModel model);
                        return model.rounds.Sum(r => Eval(r));
                    }; break;
                case Directable.GameModel:
                    func = () => { directable.Is(out GameModel model);
                        return model.roundSets.Sum(r => Eval(r)) + model.freeplayGroups.Sum(g => Eval(g));
                    }; break;
                case Directable.BloonEmissionModel:
                    func = () => { directable.Is(out BloonEmissionModel model);
                        return Eval(BloonFromName(model.bloon));
                    }; break;
                case Directable.FreeplayBloonGroupModel:
                    func = () => { directable.Is(out FreeplayBloonGroupModel model);
                        return Eval(model.group) + model.bloonEmissions.Sum(e => Eval(e));
                    }; break;
                default:
                    throw new NotImplementedException();
            }
            return func();
        }

        public override List<DirectableModel> Produce(DirectableModel m, float? v = null, int? n = 1) { throw new NotImplementedException(); }

        public override List<DirectableModel> Produce<M>(float? v = null, int? n = 1)
        {
            var list = new List<DirectableModel> { };
            var game = GetGameModel();
            var bloons = game.bloons.Select(b => (int)Eval(b)).ToArray();
            switch (GetDirectable<M>())
            {
                case Directable.BloonModel:
                    var choice = ArgUnbounded1DKnapsack((int)v, bloons).GroupBy(i => i);
                    var fusion = Fuse(choice.Select(g => game.bloons[g.Key]));
                    var count = choice.Sum(g => g.Count());
                    for (int i = 0; i < count; i++) list.Add(fusion);
                    break;
                case Directable.BloonGroupModel:
                    break;
                case Directable.RoundModel:
                    break;
                case Directable.RoundSetModel:
                    break;
                case Directable.GameModel:
                    break;
                case Directable.BloonEmissionModel:
                    break;
                case Directable.FreeplayBloonGroupModel:
                    break;
            }
            return list;
        }
    }

}