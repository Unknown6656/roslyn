// Fully expressed concept-based Eq.
// Remember to reference ConceptAttributes.dll!

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

class Tester<A> where EqA: Eq<A>
{
    int _num;
    A[] _l;
    A[] _r;
    bool _expected;

    public Tester(int num, A[] l, A[] r, bool expected)
    {
        _num = num;
        _l = l;
        _r = r;
        _expected = expected;
    }

    public void Test()
    {
        System.Console.Out.Write($"{_num}: ");
        System.Console.Out.WriteLine((default(EqArray<A, EqA>).Equals(_l, _r) == _expected) ? "pass" : "fail");
    }
}

class Program {
   static void Main()
   {
        new Tester<int, EqInt>(1, new int[] { }, new int[] { }, true).Test();
        new Tester<int, EqInt>(2, new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 }, true).Test();
        new Tester<int, EqInt>(3, new int[] { 1, 2, 3 }, new int[] { 1, 2 }, false).Test();
        new Tester<int, EqInt>(4, new int[] { 1, 2, 3 }, new int[] { 4, 5, 6 }, false).Test();
   }
}

