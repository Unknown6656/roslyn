
using System;

class ConceptAttribute : System.Attribute {}

interface I { 
}

concept J {

}

[Concept]
interface J2
{

}

class Program {
   static void Main() {
   }
  
}

