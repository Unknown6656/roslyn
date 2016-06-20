// Examples based on Bruno C. d. S. Oliveira, Adriaan Moors and Martin
// Odersky's paper, 'Type Classes as Objects and Implicits'.
using System.Collections.Generic;

/// <summary>
/// The Ord examples at the start of the paper, as well as Figure 2.
/// </summary>
namespace OrdExamples
{
    static class Ord
    {
        public static List<T> Sort<T>(List<T> xs) where OrdT : Ord<T>
        {
            // Unlike the paper, we give an implementation of Sort.

            T[] a = xs.ToArray();
            Qsort(a, 0, a.Length - 1);
            return new List<T>(a);
        }

        private static void Qsort<T>(T[] xs, int lo, int hi) where OrdT : Ord<T>
        {
            if (lo < hi)
            {
                var p = Partition(xs, lo, hi);
                Qsort(xs, lo, p - 1);
                Qsort(xs, p + 1, hi);
            }
        }

        private static int Partition<T>(T[] xs, int lo, int hi) where OrdT : Ord<T>
        {
            var pivot = xs[hi];
            var i = lo - 1;
            for (int j = lo; j < hi; j++)
            {
                if (Compare(xs[j], pivot))
                {
                    i++;
                    var tmp1 = xs[i];
                    xs[i] = xs[j];
                    xs[j] = tmp1;
                }
            }

            var tmp2 = xs[i + 1];
            xs[i + 1] = xs[hi];
            xs[hi] = tmp2;

            return i + 1;
        }

        public static T Pick<T>(T a1, T a2) where OrdA : Ord<T> => OrdA.Compare(a1, a2) ? a2 : a1;
    }

    concept Ord<T>
    {
        bool Compare(T a, T b);
    }

    public instance IntOrd : Ord<int>
    {
        bool Compare(int a, int b) => a <= b;
    }

    //
    // Figure 2
    //

    public struct Apple
    {
        public int x;
    }

    public instance OrdApple : Ord<Apple>
    {
        bool Compare(Apple a, Apple b) => a.x <= b.x;
    }

    public instance OrdApple2 : Ord<Apple>
    {
        bool Compare(Apple a, Apple b) => a.x > b.x;
    }
}

/// <summary>
/// This corresponds roughly to figure 1 of the paper, and is separate from
/// the other examples so we can use 'using static' in the driver without
/// polluting A and B with each other's monoids.
/// </summary>
namespace MonoidExamples
{
    using static Monoid;

    concept Monoid<A>
    {
        A BinaryOp(A x, A y);
        A Identity();
    }

    static class Monoid
    {
        public static A Acc<A>(List<A> l) where M : Monoid<A>
        {
            // We don't have left folds!
            A result = M.Identity();
            foreach (A a in l)
            {
                result = M.BinaryOp(result, a);
            }
            return result;
        }
    }

    static class A
    {
        public instance SumMonoid : Monoid<int>
        {
            int BinaryOp(int x, int y) => x + y;
            int Identity() => 0;
        }

        public static int Sum(List<int> l) => Acc(l);
    }

    static class B
    {
        public instance ProdMonoid : Monoid<int>
        {
            int BinaryOp(int x, int y) => x * y;
            int Identity() => 1;
        }

        public static int Product(List<int> l) => Acc(l);
    }
}

/// <summary>
/// Main example driver.
/// </summary>
namespace TCOIExamples
{
    using System;

    // This pulls in the instances (VS reports this as unnecessary!!).
    using OrdExamples;
    using static OrdExamples.Ord;

    // For Acc.
    using static MonoidExamples.Monoid;

    // This is as close as we get to Scala's import keyword.
    using static MonoidExamples.A;
    using static MonoidExamples.B;

    class Program
    {
        static void PrintList<A>(List<A> args)
        {
            Console.Out.Write("List(");
            if (0 < args.Count)
            {
                Console.Out.Write(args[0].ToString());
                for (int i = 1; i < args.Count; i++)
                {
                    Console.Out.Write(", ");
                    Console.Out.Write(args[i].ToString());
                }
            }
            Console.Out.WriteLine(")");
        }

        static void Main(string[] args)
        {
            Console.Out.Write("> sort(List(3, 2, 1) = ");
            PrintList(Sort(new List<int> { 3, 2, 1 }));

            Console.Out.WriteLine();

            // Figure 1: Locally scoped implicits.
            //
            // This is less compelling than Scala because we don't have Scala's
            // import method.  We have 'using static', but we have to do
            // namespace-fu to allow the monoid instances to be public while not
            // importing each other by accident.

            Console.Out.WriteLine("> l = List(1, 2, 3, 4, 5)");
            var l = new List<int> { 1, 2, 3, 4, 5 };
            Console.Out.WriteLine($"> Sum(l) = {Sum(l)}");
            Console.Out.WriteLine($"> Product(l) = {Product(l)}");
            Console.Out.WriteLine($"> Acc(ProdMonoid) (l) = {Acc<int, ProdMonoid>(l)}");

            Console.Out.WriteLine();

            // Figure 2: Apples to Apples with the CONCEPT pattern.

            var a1 = new Apple { x = 3 };
            var a2 = new Apple { x = 5 };
            var a3 = Pick<Apple, OrdApple>(a1, a2);
            Console.Out.WriteLine($"> Pick(apple {a1.x}, apple {a2.x})(OrdApple) = apple {a3.x}");
            var a4 = Pick<Apple, OrdApple2>(a1, a2);
            Console.Out.WriteLine($"> Pick(apple {a1.x}, apple {a2.x})(OrdApple2) = apple {a4.x}");


            Console.In.ReadLine();
        }
    }
}
