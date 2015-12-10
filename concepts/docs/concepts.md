A natural representation for type classes in .NET
Claudio Russo
Title: A natural representation for type classes in .NET
Abstract:
Type classes are an immensely popular and productive feature of Haskell. I’ll sketch that they have a natural and efficient representation in .NET. This paves the way for the extension of C# and other .NET languages with Haskell style type classes. The representation is precise and promises easy and safe cross-language interoperation.

#Haskell Type Classes
*	Type Classes capture common sets of operations. 
*	A type may be an instance of a type class, and will have a method corresponding to each operation. 
*	Type Classes may be arranged hierarchically forming notions of:
	 super classes and sub classes, and
	 permitting inheritance of operations/methods. 
*	A default method may also be associated with an operation.
Executive Summary
*	We can add type classes to C# (VB/F#). (Everyone else has them now...)
*	NO CLR CHANGES REQUIRED.
*	Only small (?) compiler changes.
*	No loss of information (a type preserving encoding).
*	Good/Very good performance.
*	Easy interop. Encoding is so light it makes sense to less classy languages too.
Type Classes in Haskell
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



#Why wasn’t this adopted?

*	Required CLR & BCL changes
*	(soundness issues)?

#Haskell “Type Classes”
 
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

#Haskell Overloads


Haskell Instances

Derived Instances
Derived Operations 
Example: Numeric types
Inheritance
Subsumption
Classy QuickSort
Performance  (Variations of QuickSort)

Disassembly
Summary
Adding Linguistic Support
Syntactic Support
Take Home
*	Haskell 98’s type classes have a type preserving .NET representation.
*	Dictionaries must be manually constructed and provided 
(a modified C#/F# compiler could do this for the user.)
*	Generated code is efficient:
*	Dictionaries are empty (stack-allocated) structs. 
   (Dictionary allocation has zero runtime cost).
*	CLR’s code specialization ensures all dictionary calls are direct calls at runtime. (In principle, these calls could be in-lined by the JIT compiler)
*	 Hackathon 2016/Intern project: type classes for C#/F#


