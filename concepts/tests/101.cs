
using System;

class ConceptAttribute : System.Attribute {}
class ConceptInstanceAttribute : System.Attribute {}

interface I { 
}

concept J<T> {

}

[Concept]
interface J2<T>
{

}

instance JInt : J<int> {
}

[ConceptInstance]
struct J2Int : J2<int>
{
}

class Program {
   static void Main() {
   }
  
}

