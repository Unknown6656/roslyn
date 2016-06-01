using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceNamedTypeSymbol
    {
        /// <summary>
        /// Gets the maximum number of possible implicit type parameters in
        /// this symbol.
        /// </summary>
        /// <returns>
        /// The number of constraints, which is an upper bound on the number
        /// of implicit parameters, if this symbol was declared as an instance
        /// in any source references.
        /// Otherwise, 0.
        /// </returns>
        private int MaxInstanceImplicits()
        {
            var maxImplicits = 0;
            // Find out the maximum number of referenced constraints.
            // There must be a better way...
            foreach (var syntaxRef in this.SyntaxReferences)
            {
                var typeDecl = (CSharpSyntaxNode)syntaxRef.GetSyntax();
                if (typeDecl.IsKind(SyntaxKind.InstanceDeclaration))
                {
                    var instDecl = (InstanceDeclarationSyntax)typeDecl;
                    maxImplicits = System.Math.Max(maxImplicits, instDecl.ConstraintClauses.Count);
                }
            }

            return maxImplicits;
        }

        /// <summary>
        /// Adds implict type parameters on an instance declaration.
        /// </summary>
        /// <param name="diagnostics">
        /// The bag of diagnostics into which we report errors.
        /// </param>
        /// <param name="instDecl">
        /// The instance declaration to examine.
        /// </param>
        /// <param name="parameterBuilder">
        /// The type parameter builder to append onto.
        /// </param>
        /// <param name="typeParameterNames">
        /// The names of existing, resolved type parameter names, plus space to
        /// install the names of the implicit parameters.
        /// </param>
        /// <param name="typeParameterVarianceKeywords">
        /// The names of existing, resolved variance keywords, plus space to
        /// install the names of the implicit parameters.
        /// </param>
        /// <param name="typeParameterMismatchReported">
        /// Whether a type parameter mismatch has been reported.
        /// </param>
        /// <param name="lastTypeParameter">
        /// The index of the last explicit type parameter found.
        /// </param>
        private void ResolveImplicitInstanceParams(DiagnosticBag diagnostics,
            InstanceDeclarationSyntax instDecl,
            ref List<AbstractTypeParameterBuilder> parameterBuilder,
            ref string[] typeParameterNames,
            ref string[] typeParameterVarianceKeywords,
            ref bool typeParameterMismatchReported,
            int lastTypeParameter)
        {
            var i = lastTypeParameter;
            
            foreach (var clause in instDecl.ConstraintClauses)
            {
                var clauseName = clause.Name.Identifier.ValueText;

                // We only capture this if it hasn't already appeared as a
                // type parameter.
                var boundAlready = false;
                for (int j = 0; j < lastTypeParameter; j++)
                {
                    if (typeParameterNames[j] == clauseName)
                    {
                        boundAlready = true;
                        break;
                    }
                }
                if (boundAlready) continue;

                // This mostly shadows the existing code in SourceNamedTypeSymbol.
                // This time, we start at the end of where that code left off.

                var name = typeParameterNames[i];
                // We've just made this type parameter up.
                var location = Location.None;
                var varianceKind = typeParameterVarianceKeywords[i];

                // Is this the first reference we've seen to this explicit parameter?
                if (name == null)
                {
                    name = typeParameterNames[i] = clauseName;
                    varianceKind = typeParameterVarianceKeywords[i] = "";  // Invariant -- correct?
                    for (int j = 0; j < i; j++)
                    {
                        if (name == typeParameterNames[j])
                        {
                            typeParameterMismatchReported = true;
                            diagnostics.Add(ErrorCode.ERR_DuplicateTypeParameter, location, name);
                            goto next;
                        }
                    }

                    if (!ReferenceEquals(ContainingType, null))
                    {
                        var tpEnclosing = ContainingType.FindEnclosingTypeParameter(name);
                        if ((object)tpEnclosing != null)
                        {
                            // Type parameter '{0}' has the same name as the type parameter from outer type '{1}'
                            diagnostics.Add(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, location, name, tpEnclosing.ContainingType);
                        }
                    }
                next:;
                }
                else if (!typeParameterMismatchReported)
                {
                    // Note: the "this", below, refers to the name of the current class, which includes its type
                    // parameter names.  But the type parameter names have not been computed yet.  Therefore, we
                    // take advantage of the fact that "this" won't undergo "ToString()" until later, when the
                    // diagnostic is printed, by which time the type parameters field will have been filled in.
                    if (varianceKind != "")
                    {
                        // Dev10 reports CS1067, even if names also don't match
                        typeParameterMismatchReported = true;
                        diagnostics.Add(
                            ErrorCode.ERR_PartialWrongTypeParamsVariance,
                            declaration.NameLocations.First(),
                            this); // see comment above
                    }
                    else if (name != clauseName)
                    {
                        typeParameterMismatchReported = true;
                        diagnostics.Add(
                            ErrorCode.ERR_PartialWrongTypeParams,
                            declaration.NameLocations.First(),
                            this); // see comment above
                    }
                }
                parameterBuilder.Add(new InstanceImplicitTypeParameterBuilder(clauseName, this));
                i++;
            }
        }
    }

    /// <summary>
    /// Type parameter builder for instance implicit type parameters.
    /// </summary>
    internal sealed class InstanceImplicitTypeParameterBuilder : AbstractTypeParameterBuilder
    {
        private readonly string _name;
        private readonly SourceNamedTypeSymbol _owner;

        /// <summary>
        /// Creates a parameter builder for implicit type parameters.
        /// </summary>
        /// <param name="name">
        /// The name of the parameter.
        /// </param>
        /// <param name="owner">
        /// The parent of the type parameter.
        /// </param>
        internal InstanceImplicitTypeParameterBuilder(string name, SourceNamedTypeSymbol owner)
        {
            _name = name;
            _owner = owner;
        }

        // @t-mawind TODO: move somewhere else
        internal override TypeParameterSymbol MakeSymbol(int ordinal, IList<AbstractTypeParameterBuilder> builders, DiagnosticBag diagnostics)
        {
            return new SynthesizedInstanceImplicitTypeParameterSymbol(_name, ordinal, _owner);
        }
    }

    /// <summary>
    /// Type of synthesised instance implicit type parameters.
    /// </summary>
    internal sealed class SynthesizedInstanceImplicitTypeParameterSymbol : TypeParameterSymbol
    {
        // @t-mawind TODO: move somewhere else
        // This is very similar to AnonymousType.TypeParameterSymbol.
        // The main difference is that, since the implicit type parameter has
        // constraints defined on it, we honour them--until, at least, we can
        // implement proper constraint resolution for instances.

        private SourceNamedTypeSymbol _owner;
        private int _ordinal;
        private TypeParameterBounds _lazyBounds = TypeParameterBounds.Unset;
        private SymbolCompletionState _state;
        private string _name;

        /// <summary>
        /// Constructs a new SynthesizedInstanceImplicitTypeParameterSymbol.
        /// </summary>
        /// <param name="name">
        /// The name of the type parameter.
        /// </param>
        /// <param name="ordinal">
        /// The ordinal of the type parameter.
        /// </param>
        /// <param name="owner">
        /// The symbol containing this type parameter.
        /// </param>
        internal SynthesizedInstanceImplicitTypeParameterSymbol(string name, int ordinal, SourceNamedTypeSymbol owner)
        {
  
            _name = name;
            _ordinal = (short)ordinal;
            _owner = owner;
        }

        public override string Name => _name;

        public override Symbol ContainingSymbol => _owner;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override int Ordinal => _ordinal;

        public override TypeParameterKind TypeParameterKind => TypeParameterKind.Type;

        public override VarianceKind Variance => VarianceKind.None; // TODO: check?

        /// <summary>
        /// Injects synthesized attribute data for this implicit parameter.
        ///
        /// <para>
        /// This is overridden to return
        /// <see cref="AttributeDescription.ConceptWitnessAttribute"/> as an
        /// attribute.  Implicit parameters have no other attributes.
        /// </para>
        /// </summary>
        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            CSharpCompilation compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.ConceptWitnessAttribute__ctor));
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
            // TODO: this is empty in AnonymousType, is this correct for us?
        }

        // The following come over from SourceTypeParameterSymbol.
        // Would be useful to understand them, and reduce overlap.

        private NamedTypeSymbol GetDefaultBaseType()
        {
            return this.ContainingAssembly.GetSpecialType(SpecialType.System_Object);
        }

        public override bool HasConstructorConstraint
        {
            get
            {
                var constraints = this.GetDeclaredConstraints();
                return (constraints & TypeParameterConstraintKind.Constructor) != 0;
            }
        }

        /// <summary>
        /// Gets whether this parameter is constrained to be a value type.
        ///
        /// <para>
        /// Implicit instance parameters are always concept instances,
        /// which are encoded as value types: therefore this is set to true.
        /// </para>
        /// </summary>
        public override bool HasValueTypeConstraint => true;

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                var constraints = this.GetDeclaredConstraints();
                return (constraints & TypeParameterConstraintKind.ReferenceType) != 0;
            }
        }

        internal override ImmutableArray<TypeSymbol> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.ConstraintTypes : ImmutableArray<TypeSymbol>.Empty;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.Interfaces : ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.EffectiveBaseClass : this.GetDefaultBaseType();
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.DeducedBaseType : this.GetDefaultBaseType();
        }

        private TypeParameterBounds GetBounds(ConsList<TypeParameterSymbol> inProgress)
        {
            Debug.Assert(!inProgress.ContainsReference(this));
            Debug.Assert(!inProgress.Any() || ReferenceEquals(inProgress.Head.ContainingSymbol, this.ContainingSymbol));

            if (ReferenceEquals(_lazyBounds, TypeParameterBounds.Unset))
            {
                var diagnostics = DiagnosticBag.GetInstance();
                var bounds = this.ResolveBounds(inProgress, diagnostics);

                if (ReferenceEquals(Interlocked.CompareExchange(ref _lazyBounds, bounds, TypeParameterBounds.Unset), TypeParameterBounds.Unset))
                {
                    this.CheckConstraintTypeConstraints(diagnostics);
                    this.AddDeclarationDiagnostics(diagnostics);
                    _state.NotePartComplete(CompletionPart.TypeParameterConstraints);
                }

                diagnostics.Free();
            }
            return _lazyBounds;
        }

        private ImmutableArray<TypeParameterSymbol> ContainerTypeParameters
        {
            get { return _owner.TypeParameters; }
        }

        private TypeParameterBounds ResolveBounds(ConsList<TypeParameterSymbol> inProgress, DiagnosticBag diagnostics)
        {
            var constraintTypes = _owner.GetTypeParameterConstraintTypes(this.Ordinal);
            return this.ResolveBounds(this.ContainingAssembly.CorLibrary, inProgress.Prepend(this), constraintTypes, false, this.DeclaringCompilation, diagnostics);
        }

        private TypeParameterConstraintKind GetDeclaredConstraints()
        {
            return _owner.GetTypeParameterConstraints(this.Ordinal);
        }


        /// <summary>
        /// Check constraints of generic types referenced in constraint types. For instance,
        /// with "interface I&lt;T&gt; where T : I&lt;T&gt; {}", check T satisfies constraints
        /// on I&lt;T&gt;. Those constraints are not checked when binding ConstraintTypes
        /// since ConstraintTypes has not been set on I&lt;T&gt; at that point.
        /// </summary>
        private void CheckConstraintTypeConstraints(DiagnosticBag diagnostics)
        {
            var constraintTypes = this.ConstraintTypesNoUseSiteDiagnostics;
            if (constraintTypes.Length == 0)
            {
                return;
            }

            var corLibrary = this.ContainingAssembly.CorLibrary;
            var conversions = new TypeConversions(corLibrary);

            foreach (var constraintType in constraintTypes)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                constraintType.AddUseSiteDiagnostics(ref useSiteDiagnostics);

                if (!diagnostics.Add(Location.None, useSiteDiagnostics))
                {
                    constraintType.CheckAllConstraints(conversions, Location.None, diagnostics);
                }
            }
        }
    }
}
