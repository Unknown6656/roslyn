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

            var groundInstances = new HashSet<TypeSymbol>();

            // Can we just represent constraints on a predicated symbol directly by
            // the concept witnesses in their arguments?
            var predicatedInstances = new HashSet<NamedTypeSymbol>();

            FilterInstances(allInstances, ref groundInstances, ref predicatedInstances);

            bool success = true;
            foreach (int j in conceptIndices)
            {
                success = TryInferConceptWitness(j, groundInstances, predicatedInstances);

                if (!success) break;
            }


            return success;
        }

        /// <summary>
        /// Tries to infer the concept witness at the given index.
        /// </summary>
        /// <param name="index">
        /// The index of the concept witness to infer.
        /// </param>
        /// <param name="groundInstances">
        /// The set of ground instances available for this witness.</param>
        /// <param name="predicatedInstances">
        /// The set of predicated instances available for this witness.
        /// </param>
        /// <returns>
        /// True if the witness was inferred and fixed in the parameter set;
        /// false otherwise.
        /// </returns>
        private bool TryInferConceptWitness(int index, HashSet<TypeSymbol> groundInstances, HashSet<NamedTypeSymbol> predicatedInstances)
        {
            foreach (var instance in groundInstances)
            {
                MutableTypeMap mtm = null;
                // TODO: this is definitely wrong.
                if (TypeUnification.CanUnify(this._methodTypeParameters[index], instance, out mtm))
                {
                    // TODO: this is probably wrong.
                    this._fixedResults[index] = mtm.SubstituteType(instance).AsTypeSymbolOnly();

                    // TODO: ensure there isn't another witness?
                    return true;
                }
            }

            // TODO: use the predicated instances (either here or before).
            return false;
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

            for (var b = binder;
                 b != null;
                 b = b.Next)
            {
                // ContainingMember crashes if we're in a BuckStopsHereBinder.
                var container = b.ContainingMemberOrLambda;
                if (container == null) continue;

                // We can see two types of instance:
                // 1) Any instances witnessed on a method or type between us and
                //    the global namespace;
                GetConstraintWitnessInstances(container, ref instances);
                // 2) Any visible named instance.  (See below, too).
                GetNamedInstances(binder, container, ref instances);

                // The above is ok if we just want to get all instances in
                // a straight line up the scope from here to the global
                // namespace, but we also need to pull in imports too.
                foreach (var u in b.GetImports(null).Usings)
                {
                    // TODO: Do we need to recurse into nested types/namespaces?
                    GetNamedInstances(binder, u.NamespaceOrType, ref instances);
                }
            }

            return instances.ToImmutableAndFree();
        }

        /// <summary>
        /// Adds all constraint witnesses in a parent member or type to an array.
        /// </summary>
        /// <param name="container">
        /// The container symbol to query.
        /// </param>
        /// <param name="instances">
        /// The instance array to populate with witnesses.
        /// </param>
        private void GetConstraintWitnessInstances(Symbol container, ref ArrayBuilder<TypeSymbol> instances)
        {
            // Only methods and named types have constrained witnesses.
            if (container.Kind != SymbolKind.Method && container.Kind != SymbolKind.NamedType) return;

            var tps = (((container as MethodSymbol)?.TypeParameters)
                       ?? (container as NamedTypeSymbol)?.TypeParameters)
                       ?? ImmutableArray<TypeParameterSymbol>.Empty;

            foreach (var tp in tps)
            {
                if (tp.IsConceptWitness) instances.Add(tp);
            }
        }

        /// <summary>
        /// Adds all named-type instances inside a container and visible in this scope to an array.
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
        private void GetNamedInstances(Binder binder, Symbol container, ref ArrayBuilder<TypeSymbol> instances)
        {
            var ignore = new HashSet<DiagnosticInfo>();

            // Only namespaces and named kinds have named instances.
            if (container.Kind != SymbolKind.Namespace && container.Kind != SymbolKind.NamedType) return;

            foreach (var member in ((NamespaceOrTypeSymbol)container).GetTypeMembers())
            {
                if (!binder.IsAccessible(member, ref ignore, binder.ContainingType)) continue;

                // Assuming that instances don't contain sub-instances.
                if (member.IsInstance)
                {
                    instances.Add(member);
                }
            }
        }

        /// <summary>
        /// Partitions visible instances into 'ground' instances, which can be
        /// substituted directly, and 'predicated' instances, which are
        /// expecting at least one of their type parameters to satisfy a
        /// concept constraint.
        /// </summary>
        /// <param name="instances">
        /// The set of instances found in the namespace.
        /// </param>
        /// <param name="grounds">
        /// The set to populate with ground instances.
        /// </param>
        /// <param name="predicateds">
        /// The set to populate with predicated instances.
        /// </param>
        private void FilterInstances(ImmutableArray<TypeSymbol> instances, ref HashSet<TypeSymbol> grounds, ref HashSet<NamedTypeSymbol> predicateds)
        {
            foreach (var instance in instances)
            {
                var isGround = true;

                // Type parameters are always concept witnesses, and are thus
                // ground--the existence of said witness means its instance is
                // accessible to us.
                if (!instance.IsTypeParameter())
                {
                    Debug.Assert(instance.Kind == SymbolKind.NamedType);

                    // If this is not expecting any witnesses itself, assume it
                    // is ground--this might not be true if it has non-witness
                    // constraints!
                    foreach (var tp in ((NamedTypeSymbol)instance).TypeParameters)
                    {
                        if (tp.IsConceptWitness)
                        {
                            isGround = false;
                            break;
                        }
                    }
                }

                if (isGround)
                {
                    grounds.Add(instance);
                }
                else
                {
                    Debug.Assert(instance.Kind == SymbolKind.NamedType);
                    predicateds.Add((NamedTypeSymbol)instance);
                }
            }
        }
    }
}
