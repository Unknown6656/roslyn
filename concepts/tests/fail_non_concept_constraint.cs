// This should fail, because instance EqArray is trying to constrain to an interface
// that isn't a concept.
// Remember to reference ConceptAttributes.dll!

interface Eq<A>
{
    bool Equals(A a, A b);
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

class Program {
   static void Main()
   {}
}

