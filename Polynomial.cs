using System;
using System.Collections.Generic;
using System.Linq;

namespace Combloonation
{
    public class Polynomial<T, V, C> where T : ICollection<V>, new() where C : IComparable<C>
    {

        public string scalarOp = " ";
        public string vectorOp = " ";
        public string directOp = ", ";

        public readonly Dictionary<T, C> terms;
        public readonly IEqualityComparer<T> compare;
        public readonly Func<C, C, C> add;
        public readonly Func<C, C, C> mult;

        public Polynomial(Func<C, C, C> add, Func<C, C, C> mult, IEqualityComparer<T> compare = null)
        {
            this.compare = compare; this.add = add; this.mult = mult;
            terms = compare != null ? new Dictionary<T, C>(compare) : new Dictionary<T, C>();
        }

        public Polynomial(Polynomial<T, V, C> p)
        {
            compare = p.compare; add = p.add; mult = p.mult;
            terms = compare != null ? new Dictionary<T, C>(p.terms, compare) : new Dictionary<T, C>(p.terms);
        }
        public Polynomial<T, V, C> Product(Polynomial<T, V, C> p)
        {
            //polynomial product
            var r = new Polynomial<T, V, C>(add, mult, compare);
            foreach (var i in terms.Keys)
            {
                foreach (var j in p.terms.Keys)
                {
                    var k = new T();
                    foreach (var v in i.Concat(j)) k.Add(v);
                    r.terms.TryGetValue(k, out var d);
                    r.terms[k] = add(mult(terms[i], p.terms[j]), d);
                }
            }
            return r;
        }

        public Polynomial<T, V, C> Cull()
        {
            //cull lower order terms
            var r = new Polynomial<T, V, C>(this);
            int n = terms.Keys.Max(k => k.Count);
            foreach (var k in terms.Keys)
            {
                if (k.Count > 0 && k.Count < n)
                {
                    r.terms.Remove(k);
                }
            }
            return r;
        }

        public Polynomial<T, V, C> BoundAbove(C b)
        {
            //bound coefficients above by b
            var r = new Polynomial<T, V, C>(this);
            foreach (var k in terms.Keys)
            {
                if (b.CompareTo(terms[k]) < 0) r.terms[k] = b;
            }
            return r;
        }

        public Polynomial<T, V, C> BoundBelow(C b)
        {
            //bound coefficients below by b
            var r = new Polynomial<T, V, C>(this);
            foreach (var k in terms.Keys)
            {
                if (b.CompareTo(terms[k]) > 0) r.terms[k] = b;
            }
            return r;
        }

        public Dictionary<T, C> Terms()
        {
            var r = new Dictionary<T, C>(terms);
            foreach (var k in terms.Keys)
            {
                if (k.Count == 0)
                {
                    r.Remove(k);
                }
            }
            return r;
        }

        public override string ToString()
        {
            return string.Join(directOp, Terms().Select(p => (p.Value + scalarOp) + string.Join(vectorOp, p.Key)));
        }
    }

    public class Combinomial<V> : Polynomial<HashSet<V>, V, int>
    {
        public Combinomial() : base((a, b) => a + b, (a, b) => a * b, HashSet<V>.CreateSetComparer())  { }
        public Combinomial(Polynomial<HashSet<V>,V,int> p) : base(p) { }

        public Combinomial(IEnumerable<V> forms) : this()
        {
            terms[new HashSet<V> { }] = 1;
            foreach (var form in forms)
            {
                var k = new HashSet<V> { form };
                terms.TryGetValue(k, out int d);
                terms[k] = 1 + d;
            }
        }

        public Combinomial(IEnumerable<Tuple<V, int>> forms) : this()
        {
            terms[new HashSet<V> { }] = 1;
            foreach (var form in forms)
            {
                var k = new HashSet<V> { form.Item1 };
                terms.TryGetValue(k, out int d);
                terms[k] = form.Item2 + d;
            }
        }

        public Combinomial<V> Product(Combinomial<V> p)
        {
            return new Combinomial<V>(base.Product(p));
        }

        public new Combinomial<V> Cull()
        {
            return new Combinomial<V>(base.Cull());
        }

        public new Combinomial<V> BoundAbove(int b)
        {
            return new Combinomial<V>(base.BoundAbove(b));
        }

        public new Combinomial<V> BoundBelow(int b)
        {
            return new Combinomial<V>(base.BoundBelow(b));
        }
    }

    public class Ordinomial<V> : Polynomial<List<V>, V, int>
    {
        public Ordinomial() : base((a, b) => a + b, (a, b) => a* b) { }
        public Ordinomial(Polynomial<List<V>, V, int> p) : base(p) { }

        public Ordinomial(IEnumerable<V> forms) : this()
        {
            terms[new List<V> { }] = 1;
            foreach (var form in forms)
            {
                var k = new List<V> { form };
                terms.TryGetValue(k, out int d);
                terms[k] = 1 + d;
            }
        }

        public Ordinomial(IEnumerable<Tuple<V, int>> forms) : this()
        {
            terms[new List<V> { }] = 1;
            foreach (var form in forms)
            {
                var k = new List<V> { form.Item1 };
                terms.TryGetValue(k, out int d);
                terms[k] = form.Item2 + d;
            }
        }

        public Ordinomial<V> Product(Ordinomial<V> p)
        {
            return new Ordinomial<V>(base.Product(p));
        }

        public new Ordinomial<V> Cull()
        {
            return new Ordinomial<V>(base.Cull());
        }

        public new Ordinomial<V> BoundAbove(int b)
        {
            return new Ordinomial<V>(base.BoundAbove(b));
        }

        public new Ordinomial<V> BoundBelow(int b)
        {
            return new Ordinomial<V>(base.BoundBelow(b));
        }
    }
}
