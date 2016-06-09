using Microsoft.CodeAnalysis.CSharp.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class MethodTypeInferrer
    {
        /// <summary>
        /// Performs the concept phase of type inference.
        /// <para>
        /// This phase occurs when the vanilla C# first and second phases have
        /// both failed.
        /// </para>
        /// <para>
        /// In this phase, we check to see whether the remaining unbound
        /// type parameters are concept witnesses.  If they are, then we
        /// find all currently visible implementations of the witnessed
        /// concept in scope, and check whether the set of implementations
        /// yields a viable type for the missing argument.
        /// </para>
        /// </summary>
        /// <param name="binder">
        /// The binder for the scope in which the type-inferred method
        /// resides.
        /// </param>
        /// <param name="useSiteDiagnostics">
        /// The diagnostics set for this use site.
        /// </param>
        /// <returns></returns>
        private bool InferTypeArgsConceptPhase(Binder binder, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // We shouldn't try this phase if we succeeded during the last one.
            Debug.Assert(!AllFixed());

            // First, make sure every unfixed type parameter is a concept, and
            // that we know where they all are so we can infer them later.
            var conceptIndexBuilder = new ArrayBuilder<int>();
            if (!GetUnfixedConceptWitnesses(ref conceptIndexBuilder)) return false;
            var conceptIndices = conceptIndexBuilder.ToImmutableAndFree();

            // If we got this far, we should have at least something to infer.
            Debug.Assert(!conceptIndices.IsEmpty);

            // Ideally this should be cached at some point, perhaps on the compilation
            // or binder.
            var allInstances = GetAllVisibleInstances(binder);

            bool success = true;
            foreach (int j in conceptIndices)
            {
                // TODO: stuff
                success = false;

                if (!success) break;
            }


            return success;
        }

        /// <summary>
        /// Checks that every unfixed type parameter is a concept witness, and
        /// stores their indices into an array.
        /// </summary>
        /// <param name="indices">
        /// The array-builder of unfixed concept witnesses.
        /// </param>
        /// <returns>
        /// True if, and only if, every unfixed type parameter is a concept
        /// witness.
        /// </returns>
        private bool GetUnfixedConceptWitnesses(ref ArrayBuilder<int> indices)
        {
            for (int i = 0; i < _methodTypeParameters.Length; i++)
            {
                if (IsUnfixed(i))
                {
                    if (!_methodTypeParameters[i].IsConceptWitness) return false;
                    indices.Add(i);
                }
            }
            return true;
        }

        /// <summary>
        /// Gets a list of all instances in scope at the given binder.
        /// </summary>
        /// <param name="binder">
        /// The binder providing scope for this query.
        /// </param>
        /// <returns>
        /// An immutable array of symbols (either type parameters or named
        /// types) representing concept instances available in the scope
        /// of <paramref name="binder"/>.
        /// </returns>
        private ImmutableArray<TypeSymbol> GetAllVisibleInstances(Binder binder)
        {
            var instances = new ArrayBuilder<TypeSymbol>();

            // We can see two types of instance:
            // 1) Any instances witnessed on a method or type between us and
            //    the global namespace;
            GetConstraintWitnessInstances(binder, ref instances);
            // 2) Any visible named instance.
            GetNamedInstances(binder, binder.Compilation.GlobalNamespace, ref instances);

            return instances.ToImmutableAndFree();
        }

        /// <summary>
        /// Adds all constraint witnesses visible in this scope to an array.
        /// </summary>
        /// <param name="binder">
        /// The binder providing scope for this query.
        /// </param>
        /// <param name="instances">
        /// The instance array to populate with witnesses.
        /// </param>
        private void GetConstraintWitnessInstances(Binder binder, ref ArrayBuilder<TypeSymbol> instances)
        {
            var parent = binder.ContainingMember();
            Debug.Assert(parent != null);

            for (var container = parent;
                 container.Kind != SymbolKind.Namespace;
                 container = container.ContainingSymbol)
            {
                // Only methods and named kinds have constrained witnesses.
                if (container.Kind != SymbolKind.Method && container.Kind != SymbolKind.NamedType) continue;

                var tps = (((container as MethodSymbol)?.TypeParameters)
                           ?? (container as NamedTypeSymbol)?.TypeParameters)
                           ?? ImmutableArray<TypeParameterSymbol>.Empty;

                foreach (var tp in tps)
                {
                    if (tp.IsConceptWitness) instances.Add(tp);
                }
            }
        }

        /// <summary>
        /// Adds all named-type instances visible in this scope to an array.
        /// </summary>
        /// <param name="binder">
        /// The binder providing scope for this query.
        /// </param>
        /// <param name="container">
        /// The current container being searched for instanes.
        /// </param>
        /// <param name="instances">
        /// The instance array to populate with witnesses.
        /// </param>
        private void GetNamedInstances(Binder binder, NamespaceOrTypeSymbol container, ref ArrayBuilder<TypeSymbol> instances)
        {
            var ignore = new HashSet<DiagnosticInfo>();

            if (container.Kind == SymbolKind.Namespace)
            {
                foreach (var member in ((NamespaceSymbol) container).GetNamespaceMembers())
                {
                    // Is this too lenient?
                    // It seems to allow us to infer instances from far-away namespaces.
                    if (!binder.IsAccessible(member, ref ignore, binder.ContainingType)) continue;
                    GetNamedInstances(binder, member, ref instances);
                }
            }

            foreach (var member in container.GetTypeMembers())
            {
                if (!binder.IsAccessible(member, ref ignore, binder.ContainingType)) continue;

                // Assuming that instances don't contain sub-instances.
                if (member.IsInstance)
                {
                    instances.Add(member);
                }
                else
                {
                    GetNamedInstances(binder, member, ref instances);
                }
            }
        }
    }
}
