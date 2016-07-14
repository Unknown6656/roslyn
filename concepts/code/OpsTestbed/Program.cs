using System;
using System.Concepts.OpPrelude;

namespace OpsTestbed
{
    class Program
    {
        static A M<A>(A x) where NumA : Num<A> => FromInteger(666) * x * x * x + FromInteger(777) * x * x + FromInteger(888);

        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Launch();
            System.Diagnostics.Debugger.Break();

            Console.WriteLine(M(255)); // int
            Console.WriteLine(M(255.0)); // double
        }
    }
}

