using System;
using System.Collections.Generic;
using System.Linq;

namespace Combloonation
{
    public class Combinomial
    {

        public Dictionary<HashSet<string>, int> terms = new Dictionary<HashSet<string>, int>(HashSet<string>.CreateSetComparer());
        public string scalarOp = " ";
        public string vectorOp = "_";
        public string directOp = " + ";

        public Combinomial()
        {

        }

        public Combinomial(IEnumerable<string> bloons)
        {
            terms[new HashSet<string> { }] = 1;
            foreach (string bloon in bloons)
            {
                var k = new HashSet<string> { bloon };
                terms.TryGetValue(k, out int d);
                terms[k] = 1 + d;
            }
        }

        public Combinomial(Combinomial p)
        {
            terms = new Dictionary<HashSet<string>, int>(p.terms);
        }
        public Combinomial Product(Combinomial p)
        {
            //polynomial product
            var r = new Combinomial();
            foreach (var i in terms.Keys)
            {
                foreach (var j in p.terms.Keys)
                {
                    var k = new HashSet<string>(i.Concat(j));
                    r.terms.TryGetValue(k, out int d);
                    r.terms[k] = Math.Max(0, terms[i] * p.terms[j] + d);
                }
            }
            return r;
        }

        public Combinomial Cull()
        {
            //cull lower order terms
            var r = new Combinomial(this);
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

        public Combinomial Bound(int n = 1)
        {
            //bound coefficients
            var r = new Combinomial(this);
            foreach (var k in terms.Keys)
            {
                r.terms[k] = Math.Min(n, terms[k]);
            }
            return r;
        }

        public Dictionary<HashSet<string>, int> Terms()
        {
            var r = new Dictionary<HashSet<string>, int>(terms);
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
            return string.Join(directOp, Terms().Select(p => (p.Value != 1 ? (p.Value + scalarOp) : "") + string.Join(vectorOp, p.Key)));
        }
    }
}
