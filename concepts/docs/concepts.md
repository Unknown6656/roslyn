#A natural representation for type classes in .NET


Claudio Russo

##Abstract:

Type classes are an immensely popular and productive feature of Haskell. I’ll sketch that they have a natural and efficient representation in .NET. This paves the way for the extension of C# and other .NET languages with Haskell style type classes. The representation is precise and promises easy and safe cross-language interoperation.

##Background

###Haskell Type Classes
*	Type Classes capture common sets of operations. 
*	A type may be an instance of a type class, and will have a method corresponding to each operation. 
*	Type Classes may be arranged hierarchically forming notions of:
	 super classes and sub classes, and
	 permitting inheritance of operations/methods. 
*	A default method may also be associated with an operation.

###Executive Summary
*	We can add type classes to C# (VB/F#). (Everyone else has them now...)
*	NO CLR CHANGES REQUIRED.
*	Only small (?) compiler changes.
*	No loss of information (a type preserving encoding).
*	Good/Very good performance.
*	Easy interop. Encoding is so light it makes sense to less classy languages too.

##Type Classes for CLI

Why didn’t we do this before?

Times have changed: not just Haskell anymore…
*	Swift protocols
*	Scala implicits
*	Rust	traits	
*	Go structural interfaces
*	Academic proposals: JavaGI, Static Interface Methods for the CLR
*	Isabelle, Coq, LEAN (theorem provers)
*	 C++ concepts
*	...


## Compare with: "Static Interface Methods for the CLR (Eidt & Detlefs)"

Why wasn’t this adopted?

*	Required CLR & BCL changes
*	(soundness issues)

This approach requires *no* changes to CLR or BCL (compiler changes + conventions only).
It's *sound by construction*.

## Haskell Type Classes
 
We represent Haskell type classes as Generic interfaces.

```Haskell
  class Eq a where 
    (==)                  :: a -> a -> Bool

```

```csharp
  interface Eq<A>
  {
    bool Equals(A a, A b);
  }
```

##Haskell Overloads

The Haskell declaration of class `Eq a` implicitly declares the overloaded 
operations induced by class `Eq a`’s members.

```Haskell
    (==)                    :: (Eq a) => a -> a -> Bool 
```

```csharp
  static class Overloads
  {
    public static bool Equals<EqA, A>(A a, A b) where EqA : struct, Eq<A>
    {
      return default(EqA).Equals(a, b);
    }
  }
```

An operation over some class is a static generic method, parameterized by an additional dictionary type parameter (EqA).

* Haskell dictionary value ~ C# dictionary type

The dictionary type parameter is marked "struct” (so stack allocated):
we can access its operations through a default value (no need to pass dictionary values).



##Haskell Instances

A Haskell ground instance, eg.

```Haskell
  instance Eq Integer where 
    x == y                =  x `integerEq` y

  instance Eq Float where
    x == y                =  x `floatEq` y
```

is translated to a non-generic struct implementing the appropriate type class interface.
 
```csharp
  struct EqInt : Eq<int>
  {
    public bool Equals(int a, int b) { return a == b; }
  }

  struct EqFloat : Eq<float>
  {
    public bool Equals(float a, float b) { return a == b; }
  }
```

###Derived Instances

We can represent a Haskell *parameterized instance* as a *generic struct*, 
implementing an interface but parameterized by suitably constrained type parameters. 

```Haskell
  instance (Eq a) => Eq ([a]) where 
    nil        == nil                 = true
    (a:as) == (b:bs)                  =  (a == b) && (as == bs)
  This Haskell code defines, given an equality on type a’s (any a) an equality operation on type list of a, written [a].
```

  Substituting, for simplicity, arrays for lists in CS we can write: 

```csharp
  struct EqArray<A, EqA> : Eq<A[]> where EqA : struct, Eq<A>
  {
    public bool Equals(A[] a, A[] b) {
      if (a == null) return b == null;
      if (b == null) return false;
      if (a.Length != b.Length) return false;
      for (int i = 0; i < a.Length; i++)
        if (!Overloads.Equals<EqA, A>(a[i], b[i])) return false;
      return true;
    }
  }
```



### Derived Operations 

We translate Haskell’s qualified types as extra type parameters, constrained to be both structs and bound by translations of their type class constraints.

For example, equality based list membership in Haskell is defined as follows:

```Haskell
  elem :: Eq a => a -> [a] -> bool
  x `elem`  []            = False
  x `elem` (y:ys)         = x==y || (x `elem` ys)  
``` 

In C#, we can define:

```csharp
  static bool Elem<EqA, A>(A x, A[] ys) where EqA : struct, Eq<A> {
      for (int i = 0; i < ys.Length; i++)
      {
        if (Overloads.Equals<EqA, A>(x, ys[i])) return true;
      }
      return false;
  }
```

### Example: Numeric types

```csharp
interface Num<A> {
    A Add(A a, A b);
    A Mult(A a, A b);
    A Neg(A a);
  }

  static class Overloads {
    public static A Add<NumA, A>(A a, A b) where NumA : struct, Num<A> {
      return default(NumA).Add(a, a); }
    public static A Mult<NumA, A>(A a, A b) where NumA : struct, Num<A> {
      return default(NumA).Mult (a, a); }
    public static A Neg<NumA, A>(A a) where NumA : struct, Num<A> {
      return default(NumA).Neg(a);
    }
  }

  struct NumInt : Num<int> {
    int Num<int>.Add(int a, int b) { return a + b; }
    int Num<int>.Mult(int a, int b) { return a * b; }
    int Num<int>.Neg(int a) { return ~a; }
  }
```

### Inheritance

```csharp
  using Eq;

  interface Num<A> : Eq<A> {
    A Add(A a, A b);
    A Mult(A a, A b);
    A Neg(A a);
  }

  struct NumInt : Num<int> {
    public bool Equals(int a, int b) { return default(EqInt).Equals(a, b); }
    public int Add(int a, int b) { return a + b; }
    public int Mult(int a, int b) { return a * b; }
    public int Neg(int a) { return ~a; }
  }
```

* Forall types `A`, `Num<A> : Eq<A>`

* Haskell class inheritance ~ C# interface inheritance 

### Subsumption

```csharp
    static bool Equals<EqA, A>(A a, A b) where EqA : struct, Eq<A> {
      EqA eqA = default(EqA);
      return eqA.Equals(a, b);
    }

    static A Square<NumA, A>(A a) where NumA : struct, Num<A> {
      return default(NumA).Mult(a, a);
    }

    static bool MemSq<NumA, A>(A[] a_s, A a)
         where NumA : struct, Num<A> {
      for (int i = 0; i < a_s.Length; i++) {
        if (Equals<NumA, A>(a_s[i], Square<NumA, A>(a))) return true;
               /*  ^^^^ legal only because NumA : Num<A> : Eq<A> */
      }
      return false;
    }
```

##Performance

TBC (need to import existing charts)

###Classy QuickSort

```csharp
    // Polymorphic OO-style quicksort: general, typesafe
    // Note the type parameter bound in the generic method

    public static void qsort<IOrdT, T>(T[] arr, int a, int b)
      where IOrdT : IOrd<T> 
   {
      IOrdT iordt = default(IOrdT);
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        T x = arr[(i + j) / 2];
        do {
          while (iordt.Compare(arr[i], x) < 0) i++;
          while (iordt.Compare(x, arr[j]) < 0) j--;
          if (i <= j) {
            swap<T>(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort<IOrdT, T>(arr, a, j);
        qsort<IOrdT, T>(arr, i, b);
      }
    }
```


###Performance  (Variations of QuickSort)

###Disassembly

```csharp
public static bool Equals<EqA,A>(A a, A b) 
      where EqA : struct, Eq<A>
    {
      return default(EqA).Equals(a, b);
    }
```

```CIL
.method public hidebysig static     bool Equals<valuetype .ctor ([mscorlib]System.ValueType, class Eq.Eq`1<!!A>) EqA, A> 
    // dictionary EqA is a type argument (not at value) 
        (!!A a,!!A b 
   ) cil managed { 
   .locals init (        [0] !!EqA loc1,        [1] !!EqA loc2)
   IL_0000: ldloca.s loc1  // stack allocation of default struct (actually an empty token)    
   IL_0002: initobj !!EqA     
   IL_0008: ldloc.0    
   IL_0009: stloc.1    
   IL_000a: ldloca.s loc2    
   IL_000c: ldarg.0    
   IL_000d: ldarg.1    
   IL_000e: constrained. !!EqA  // a direct call to an interface method on that struct    
   IL_0014: callvirt instance bool class Eq.Eq`1<!!A>::Equals(!0, !0)   
   IL_0019: ret
} 

```

###Summary

| Haskell | C#|
----------|--------
|Type Class	| Generic Interface|
|Type Class instance	| Struct|
|Derived Class |	Generic Struct|
|Overloaded Operation | Constrained Generic Method |
|Implicit Dictionary Construction | Explicit Dictionary Construction |
|Implicit Dictionary Passing	Explicit | Dictionary Passing |
|Type  Class Inheritance	| Interface Inheritance |




### Syntactic Support

* Distinguish type class declarations (new keyword concept)
* Anonymize instance declarations (new keyword instance)
* Add implicit dictionary type abstraction (induced by type class constraints)
* Add implicit dictionary type instantiation (similar to type argument inference)


Before:

```csharp
  interface Eq<A> { 
    bool Equals(A a, A b);
  }

  struct EqInt : Eq<int> {
    bool Equals(int a, int b) { return a == b; }
  }

  struct EqArray<EqA,A>: Eq<A[]> where EqA: struct, Eq<A> {
    bool Equals(A[] a, A[] b) {
      var dict = default(EqA);
      if (a == null) return b == null;
      if (b == null) return false;
      if (a.Length != b.Length) return false;
      for (int i = 0; i < a.Length; i++)
        if (dict.Equals(a[i], b[i])) return false;
      return true;
    }
 }
```

After:

```csharp
  concept Eq<A> {
    bool Equals(A a, A b);
  }

  instance Eq<int> {
    bool Equals(int a, int b) { return a == b; }
  }

  instance <A>Eq<A[]> where Eq<A> {
    bool Equals(A[] a, A[] b) {
      if (a == null) return b == null;
      if (b == null) return false;
      if (a.Length != b.Length) return false;
      for (int i = 0; i < a.Length; i++)
        if (Equals(a[i], b[i])) return false;
      return true;
    }
 }
```




##Take Home

*	Haskell 98’s type classes have a type preserving .NET representation.
*	Dictionaries must be manually constructed and provided  (a modified C#/F# compiler could do this for the user.)
*	Generated code is efficient:
    * Dictionaries are empty (stack-allocated) structs. 
    * Dictionary allocation has zero runtime cost.
    *	CLR’s code specialization ensures all dictionary calls are direct calls at runtime. (In principle, these calls could be in-lined by the JIT compiler)



