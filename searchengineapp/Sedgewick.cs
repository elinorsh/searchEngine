using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpStringSort
{
    /// <summary>
    /// https://www.codeproject.com/articles/146086/fast-string-sort-in-c-and-f
    /// open code for sorting strings
    /// its mainly for sorting the 'prePosting' dictionary so we can write it sorted for the merge
    /// </summary>
    public static class Sedgewick
    {

        static char CharOrNull(string s, int pos)
        {
            if (pos >= s.Length)
                return (char)0;
            return s[pos];
        }

        static int medianOf3(string[] x, int a, int b, int c, int depth)
        {
            char va, vb, vc;
            if ((va = CharOrNull(x[a], depth)) == (vb = CharOrNull(x[b], depth)))
                return a;
            if ((vc = CharOrNull(x[c], depth)) == va || vc == vb)
                return c;
            return va < vb ?
                  (vb < vc ? b : (va < vc ? c : a))
                : (vb > vc ? b : (va < vc ? a : c));
        }

        //Pathological case is: strings with long common prefixes will
        //          cause long running times
        public static void InsertionSort(string[] x, int a, int n, int depth)
        {
            int pi;
            int pj;
            for (pi = a + 1; --n > 0; pi++)
            {
                for (pj = pi; pj > a; pj--)
                {
                    string s = x[pj - 1];
                    string t = x[pj];
                    int d = depth;

                    int s_len = s.Length;
                    int t_len = t.Length;
                    while (d < s_len && d < t_len && s[d] == t[d]) { d++; };
                    if (d == s_len || (d < t_len && s[d] <= t[d]))
                        break;
                    int pj1 = pj - 1;
                    string tmp = x[pj]; x[pj] = x[pj1]; x[pj1] = tmp;
                }
            }
        }

        static void vecswap(string[] x, int a, int b, long n)
        {

            while (n-- > 0)
            {
                string t = x[a];
                x[a++] = x[b];
                x[b++] = t;
            }
        }


        public static string[] Sort(string[] input)
        {
            string[] copy = new string[input.Length];
            input.CopyTo(copy, 0);
            InPlaceSort(copy, 0, copy.Length, 0);
            return copy;
        }


        static void InPlaceSort(string[] x, int a, int n, int depth)
        {
            char partval;
            int d, r;
            int pa;
            int pb;
            int pc;
            int pd;
            int pl;
            int pm;
            int pn;
            string t;
            if (n < 10)
            {
                InsertionSort(x, a, n, depth);
                return;
            }
            pl = a;
            pm = a + n / 2;
            pn = a + (n - 1);
            if (n > 30)
            {
                // On big arrays, pseudomedian of 9
                d = (n / 8);
                pl = medianOf3(x, pl, pl + d, pl + 2 * d, depth);
                pm = medianOf3(x, pm - d, pm, pm + d, depth);
                pn = medianOf3(x, pn - 2 * d, pn - d, pn, depth);
            }
            pm = medianOf3(x, pl, pm, pn, depth);

            { t = x[a]; x[a] = x[pm]; x[pm] = t; }

            pa = pb = a + 1;
            pc = pd = a + n - 1;

            partval = CharOrNull(x[a], depth);
            int len = x[a].Length;
            bool empty = (len == depth);

            for (;;)
            {
                while (pb <= pc && (r = (empty ? (x[pb].Length - depth) : ((depth == x[pb].Length) ? -1 : (x[pb][depth] - partval)))) <= 0)
                {
                    if (r == 0)
                    {
                        //swap(pa, pb);
                        { t = x[pa]; x[pa] = x[pb]; x[pb] = t; }
                        pa++;
                    }
                    pb++;
                }
                while (pb <= pc && (r = (empty ? (x[pc].Length - depth) : ((depth == x[pc].Length) ? -1 : (x[pc][depth] - partval)))) >= 0)
                {
                    if (r == 0)
                    {   //swap(pc, pd); 
                        { t = x[pc]; x[pc] = x[pd]; x[pd] = t; }
                        pd--;
                    }
                    pc--;
                }
                if (pb > pc) break;

                //swap(pb, pc);
                { t = x[pb]; x[pb] = x[pc]; x[pc] = t; }
                pb++;
                pc--;
            }

            pn = a + n;
            if (pa - a < pb - pa)
            {
                r = (pa - a);
            }
            else
            {
                r = (pb - pa);
            }

            //swapping pointers to strings
            vecswap(x, a, pb - r, r);
            if (pd - pc < pn - pd - 1) { r = pd - pc; } else { r = pn - pd - 1; }
            vecswap(x, pb, pn - r, r);
            r = pb - pa;
            if (pa - a + pn - pd - 1 > 1 && x[a + r].Length > depth) //by definition x[a + r] has at least one element
                InPlaceSort(x, a + r, pa - a + pn - pd - 1, depth + 1);
            if (r > 1)
                InPlaceSort(x, a, r, depth);
            if ((r = pd - pc) > 1)
                InPlaceSort(x, a + n - r, r, depth);
        }
    }
}
