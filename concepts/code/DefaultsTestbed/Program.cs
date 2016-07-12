using System.Concepts;

namespace DefaultsTestbed
{
    class Program
    {
        concept Eq<A>
        {
            bool Eq(A a, A b);
            bool Neq(A a, A b);
        }

        [ConceptDefault]
        struct Eq_default<A> where EqA : Eq<A>
        {
            public bool Eq(A a, A b) => !EqA.Neq(a, b);
            public bool Neq(A a, A b) => !EqA.Eq(a, b);
        }

        instance EqInt : Eq<int>
        {
            bool Eq(int a, int b) => a == b;
        }

        static void Main(string[] args)
        {
            System.Console.WriteLine(EqInt.Eq(27, 53) ? "f" : "p");
            //System.Console.WriteLine(EqInt.Neq(27, 53) ? "p" : "f");
        }
    }
}
