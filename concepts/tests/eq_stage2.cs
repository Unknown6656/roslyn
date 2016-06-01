// Fully expressed concept-based Eq.

class ConceptAttribute : System.Attribute {}
class ConceptInstanceAttribute : System.Attribute {}
class ConceptWitnessAttribute : System.Attribute {}

concept Eq<A>
{
    bool Equals(A a, A b);
}

instance EqInt : Eq<int>
{
    public bool Equals(int a, int b) => a == b;
}

instance EqArray<A> : Eq<A[]> where EqA: Eq<A>
{
    public bool Equals(A[] a, A[] b)
    {
        var dict = default(EqA);
        if (a == null) return b == null;
        if (b == null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (!dict.Equals(a[i], b[i])) return false;
        }
        return true;
    }
}

//
// Driver.
//

class Program {
   static void Main()
   {
        var dict = default(EqArray<int, EqInt>);
        System.Console.Out.Write("1: ");
        System.Console.Out.WriteLine(dict.Equals(new int[] {}, new int[] {}) ? "pass" : "fail");
        System.Console.Out.Write("2: ");
        System.Console.Out.WriteLine(dict.Equals(new int[] {  1, 2, 3 }, new int[] { 1, 2, 3 }) ? "pass" : "fail");
        System.Console.Out.Write("3: ");
        System.Console.Out.WriteLine(dict.Equals(new int[] { 1, 2, 3 }, new int[] { 1, 2 }) ? "fail" : "pass");
        System.Console.Out.Write("4: ");
        System.Console.Out.WriteLine(dict.Equals(new int[] { 1, 2, 3 }, new int[] { 4, 5, 6 }) ? "fail" : "pass");
    }
}

