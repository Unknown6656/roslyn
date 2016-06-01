using System;

namespace System.Concepts
{
    class ConceptAttribute : System.Attribute {}
    class ConceptInstanceAttribute : System.Attribute {}
    class ConceptWitnessAttribute : System.Attribute {}
}

//
// Normal interface and struct.
//
// These shouldn't be modified.
//

interface I<T>
{
} 

struct IInt : I<int>
{
}

//
// Concepts.
//
// These should be converted down into interfaces and structs.
//

concept J<T>
{
}

instance JInt : J<int>
{
}


//
// 'Separate compilation' desugared concepts.
//
// These should be unmodified, but work as if they were concepts.
//

[System.Concepts.Concept]
interface J2<T>
{
}


[System.Concepts.ConceptInstance]
struct J2Int : J2<int>
{
}

//
// 'Over-annotated' concepts.
//
// These should not have the annotations added twice.
//

[System.Concepts.Concept]
concept J3<T>
{
}


[System.Concepts.ConceptInstance]
instance J3Int : J3<int>
{
}

//
// Driver.
//

class Program {
   static void Main()
   {
   }
}

