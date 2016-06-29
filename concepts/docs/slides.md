# Concept CSharp

* transfer type classes (from Haskell) to C#
* implement in Roslyn compiler
* case studies & perf benchmarks

#  Haskell Type Classes (in a Nutshell)

```Haskell
  -- type class declaration
  class Eq a where 
    (==)                  :: a -> a -> Bool
  
  -- intance declarations
  instance Eq Integer where 
    x == y                =  x `integerEq` y

   -- a derived instance
  instance (Eq a) => Eq ([a]) where 
       nil == nil      = true
    (a:as) == (b:bs)   = (a == b) && (as == bs)
         _ == _        = false

  -- derived operation
  elem :: Eq a => a -> [a] -> bool
  x `elem`  []            = False
  x `elem` (y:ys)         = x==y || (x `elem` ys)  
```

C#:
```csharp
  // type class
  interface Eq<A>  {
    bool Equals(A a, A b);
  }
  // instance
  struct EqInt : Eq<int>  {
    public bool Equals(int a, int b)  => a == b; 
  }
  // derived instance
  struct EqArray<A, EqA> : Eq<A[]> where EqA : struct, Eq<A> {
    public bool Equals(A[] a, A[] b) {
      if (a.Length != b.Length) return false;
      for (int i = 0; i < a.Length; i++)
        if default(EqA).Equals(a[i], b[i])) return false;
      return true;}
  }
  // derived operation
  static bool Elem<EqA, A>(A x, A[] ys) where EqA : struct, Eq<A> {
      for (int i = 0; i < ys.Length; i++) 
        if default(EqA).Equals(x, ys[i])) return true;
      return false;
  }
```

Concept C#
```csharp
  // type class
  concept Eq<A> {
    bool Equals(A a, A b);
  }
  // instance
  instance EqInt : Eq<int> {
    public bool Equals(int a, int b)  => a == b; 
  }
  // derived instance
  instance EqArray<A> : Eq<A[]> where EqA : Eq<A> {
     bool Equals(A[] a, A[] b) {
       if (a.Length != b.Length) return false;
       for (int i = 0; i < a.Length; i++)
          if Equals(a[i], b[i])) return false;
       return true;}
  }
  // derived operation
  static bool Elem<A>(A x, A[] ys) where EqA : Eq<A> {
      for (int i = 0; i < ys.Length; i++)
        if (Equals(x, ys[i])) return true;
      return false;
  }
```

# Summary

| Haskell | C#| Concept C# |
----------|--------|--------
|type class	| generic interface| generic concept 
|instance	| struct           | instance
|derived instance | generic struct | generic instance
|type class inheritance	| interface inheritance | concept inheritance
|overloaded operation | constrained generic method | generic method with implicit type parameters
|implicit dictionary construction | explicit type construction | implicit instance construction with explicit fallback
|implicit dictionary passing | explicit type passing | implicit type passing with explicit fallback
|constraint inference & propagation | NA | NA

