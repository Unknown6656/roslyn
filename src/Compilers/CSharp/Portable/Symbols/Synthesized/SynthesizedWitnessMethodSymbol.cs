using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Symbol representing a member belonging to a concept, which has been
    /// accessed through a concept witness.
    /// <para>
    /// The main goal of this class is to mark the method so it invocations
    /// of it can be dispatched properly during binding.  This is a rather
    /// hacky way of doing this, to say the least.
    /// </para>
    /// </summary>
    internal sealed class SynthesizedWitnessMethodSymbol : MethodSymbol
    {
        /// <summary>
        /// The concept method to wrap.
        /// </summary>
        private MethodSymbol _method;

        /// <summary>
        /// The witness 'owning' the concept method.
        /// </summary>
        private TypeParameterSymbol _parent;

        /// <summary>
        /// Constructs a new <see cref="SynthesizedWitnessMethodSymbol"/>.
        /// </summary>
        /// <param name="method">
        /// The concept method to wrap.
        /// </param>
        /// <param name="parent">
        /// The witness 'owning' the concept method.
        /// </param>
        internal SynthesizedWitnessMethodSymbol(MethodSymbol method, TypeParameterSymbol parent)
        {
            Debug.Assert(parent.IsConceptWitness);

            _method = method;
            _parent = parent;
        }

        /// <summary>
        /// Gets the type parameter of the witness from which this method is
        /// being called.
        /// </summary>
        internal TypeParameterSymbol Parent => _parent;

        public override string Name => _method.Name;

        public override MethodSymbol OriginalDefinition => _method.OriginalDefinition;

        public override Symbol ContainingSymbol => _method;

        public override Accessibility DeclaredAccessibility => _method.DeclaredAccessibility;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            => ImmutableArray<SyntaxReference>.Empty;

        public sealed override bool IsImplicitlyDeclared => true;

        public override bool IsAbstract => _method.IsAbstract;

        public override bool IsExtern => _method.IsExtern;

        public override bool IsOverride => _method.IsOverride;

        public override bool IsSealed => _method.IsSealed;

        public override bool IsStatic => _method.IsStatic;

        public override bool IsVirtual => _method.IsVirtual;

        public override ImmutableArray<Location> Locations
            => ImmutableArray<Location>.Empty;

        internal override ObsoleteAttributeData ObsoleteAttributeData
            => _method.ObsoleteAttributeData;

        public override MethodKind MethodKind => _method.MethodKind;

        public override int Arity => _method.Arity;

        public override bool IsExtensionMethod => _method.IsExtensionMethod;

        internal override bool HasSpecialName => _method.HasSpecialName;

        internal override MethodImplAttributes ImplementationAttributes => _method.ImplementationAttributes;

        internal override bool HasDeclarativeSecurity => _method.HasDeclarativeSecurity;

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => _method.ReturnValueMarshallingInformation;

        internal override bool RequiresSecurityObject => _method.RequiresSecurityObject;

        public override bool HidesBaseMethodsByName => _method.HidesBaseMethodsByName;

        public override bool IsVararg => _method.IsVararg;

        public override bool ReturnsVoid => _method.ReturnsVoid;

        public override bool IsAsync => _method.IsAsync;

        public override TypeSymbol ReturnType => _method.ReturnType;

        public override ImmutableArray<TypeSymbol> TypeArguments => _method.TypeArguments;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => _method.TypeParameters;

        public override ImmutableArray<ParameterSymbol> Parameters => _method.Parameters;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => _method.ExplicitInterfaceImplementations;

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => _method.ReturnTypeCustomModifiers;

        public override Symbol AssociatedSymbol => _method.AssociatedSymbol;

        internal override CallingConvention CallingConvention => _method.CallingConvention;

        internal override bool GenerateDebugInfo => _method.GenerateDebugInfo;

        public override DllImportData GetDllImportData() => _method.GetDllImportData();

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation() => _method.GetSecurityInformation();

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => _method.GetAppliedConditionalSymbols();

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => _method.CalculateLocalSyntaxOffset(localPosition, localTree);

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => _method.IsMetadataNewSlot(ignoreInterfaceImplementationChanges);

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => _method.IsMetadataVirtual(ignoreInterfaceImplementationChanges);
    }
}
