/// <summary>
/// Attributes for the concepts system.
/// </summary>
namespace System.Concepts
{
    /// <summary>
    /// Attribute marking interfaces as concepts.
    /// <para>
    /// Syntactic concepts are reduced to interfaces with this attribute in the
    /// emitted code.  Also, interfaces with this attribute are treated as
    /// concepts by the compiler.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class ConceptAttribute : System.Attribute { }

    /// <summary>
    /// Attribute marking structs as concept instances.
    /// <para>
    /// Syntactic instances are reduced to structs with this attribute in the
    /// emitted code.  Also, structs with this attribute are treated as concept
    /// instances by the compiler.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class ConceptInstanceAttribute : System.Attribute { }

    /// <summary>
    /// Attribute marking type parameters as concept witnesses.
    /// <para>
    /// Generated witnesses are given this attribute in the emitted code.
    /// Also, type parameters with this attribute are treated as concept
    /// witnesses by the compiler.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = true)]
    public class ConceptWitnessAttribute : System.Attribute { }
}