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

        static void Main(string[] args)
        {
        }
    }
}
