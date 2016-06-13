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
            if (!GetMethodUnfixedConceptWitnesses(ref conceptIndexBuilder)) return false;
            var conceptIndices = conceptIndexBuilder.ToImmutableAndFree();

            // If we got this far, we should have at least something to infer.
            Debug.Assert(!conceptIndices.IsEmpty);

            // We'll be checking to see if concepts defined on the missing
            // witness type parameters are implemented.  Since this means we
            // are checking something on the method definition, but need it
            // in terms of our fixed type arguments, we must make a mapping
            // from parameters to arguments.
            var fixedMap = this.MakeMethodFixedMap();

            // TODO: Ideally this should be cached at some point, perhaps on the
            // compilation or binder.
            var allInstances = GetAllVisibleInstances(binder);

            var boundParams = GetAlreadyBoundTypeParameters(binder);

            bool success = true;
            foreach (int j in conceptIndices)
            {
                var maybeFixed = TryInferConceptWitness(_methodTypeParameters[j], allInstances, fixedMap, boundParams);
                if (maybeFixed == null) return false;
                Debug.Assert(maybeFixed != null && maybeFixed.IsInstanceType());
                _fixedResults[j] = maybeFixed;
            }

            return success;
        }

        #region Setup

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
        private bool GetMethodUnfixedConceptWitnesses(ref ArrayBuilder<int> indices)
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
        /// Constructs a map from fixed method type parameters to their
        /// inferred arguments.
        /// </summary>
        /// <returns>
        /// A map mapping each fixed parameter to its argument.
        /// </returns>
        private MutableTypeMap MakeMethodFixedMap()
        {
            MutableTypeMap mt = new MutableTypeMap();

            for (int i = 0; i < _methodTypeParameters.Length; i++)
            {
                if (_fixedResults[i] != null)
                {
                    mt.Add(_methodTypeParameters[i], new TypeWithModifiers(_fixedResults[i]));
                }
            }

            return mt;
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

            // TODO: find out why we're getting duplicates in the first place.
            instances.RemoveDuplicates();
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
        /// Finds all of the type parameters in a given scope that have been
        /// bound by methods or classes.
        /// </summary>
        /// <param name="binder">
        /// The binder providing scope for this query.
        /// </param>
        /// <returns>
        /// </returns>
        private ImmutableHashSet<TypeParameterSymbol> GetAlreadyBoundTypeParameters(Binder binder)
        {
            // TODO: combine with other binder traversal?
            var tps = new ArrayBuilder<TypeParameterSymbol>();

            for (var b = binder; b != null; b = b.Next)
            {
                var container = b.ContainingMemberOrLambda;
                if (container == null ||
                    !(container.Kind == SymbolKind.NamedType || container.Kind == SymbolKind.Method)) continue;

                var containertps =
                    ((container as MethodSymbol)?.TypeParameters)
                    ?? ((container as NamedTypeSymbol)?.TypeParameters)
                    ?? ImmutableArray<TypeParameterSymbol>.Empty;

                tps.AddRange(containertps);
            }

            // TODO: We're doing something wrong here that is causing duplicates.
            tps.RemoveDuplicates();
            return tps.ToImmutableAndFree().ToImmutableHashSet();
        }

        #endregion Setup
        #region Main driver

        /// <summary>
        /// Tries to infer the concept witness for the given type parameter.
        /// </summary>
        /// <param name="typeParam">
        /// The type parameter of the concept witness to infer.
        /// </param>
        /// <param name="allInstances">
        /// The set of instances available for this witness.
        /// </param>
        /// <param name="fixedMap">
        /// The map from fixed type parameters to their arguments.
        /// </param>
        /// <param name="boundParams">
        /// A list of all type parameters that have been bound by the
        /// containing scope, and thus cannot be substituted by unification.
        /// </param>
        /// <returns>
        /// Null if inference failed; else, the inferred concept instance.
        /// </returns>
        private TypeSymbol TryInferConceptWitness(TypeParameterSymbol typeParam,
            ImmutableArray<TypeSymbol> allInstances,
            MutableTypeMap fixedMap,
            ImmutableHashSet<TypeParameterSymbol> boundParams)
        {
            // @t-mawind
            // An instance satisfies inference if:
            //
            // 1) for all concepts required by the type parameter, at least
            //    one concept on the witness unifies with that concept without
            //    capturing bound type parameters;
            // 2) all of the type parameters of that instance can be bound,
            //    both by the substitutions from the unification above and also
            //    by recursively trying to infer any missing concept witnesses.
            //
            // The first part is equivalent to establishing
            //    witness :- instance.
            //
            // The second part is equivalent to resolving
            //    instance :- dependency1; dependency2; ...
            // by trying to establish the dependencies as separare queries.
            //
            // After the second part, if we have multiple possible instances,
            // we try to see if one implements a subconcept of all of the other
            // instances.  If so, we narrow to that specific instance.
            //
            // If we have multiple satisfying instances, or zero, we fail.

            // TODO: We don't yet have #2, so we presume that if we have any
            // concept-witness type parameters we've failed.

            var requiredConcepts = this.GetRequiredConceptsFor(typeParam, fixedMap);
            var firstPassInstances = this.TryInferConceptWitnessFirstPass(allInstances, requiredConcepts, boundParams);

            // We can't infer if none of the instances implement our concept!
            // However, if we have more than one candidate instance at this
            // point, we shouldn't bail until we've made sure only one of them
            // passes 2).
            if (firstPassInstances.IsEmpty) return null;

            var secondPassInstances = this.TryInferConceptWitnessSecondPass(firstPassInstances, allInstances, boundParams);

            // We only do the third pass if the second pass returned too many items.
            var thirdPassInstances = secondPassInstances;
            if (secondPassInstances.Length > 1) thirdPassInstances = this.TryInferConceptWitnessThirdPass(secondPassInstances, fixedMap);

            // Either ambiguity, or an outright lack of inference success.
            if (thirdPassInstances.Length != 1) return null;
            return thirdPassInstances[0];
        }

        /// <summary>
        /// Deduces the set of concepts that must be implemented by any witness
        /// supplied to the given type parameter.
        /// </summary>
        /// <param name="typeParam">
        /// The type parameter being inferred.
        /// </param>
        /// <param name="fixedMap">
        /// A map mapping fixed type parameters to their type arguments.
        /// </param>
        /// <returns>
        /// An array of concepts required by <paramref name="typeParam"/>.
        /// </returns>
        private ImmutableArray<TypeSymbol> GetRequiredConceptsFor(TypeParameterSymbol typeParam, MutableTypeMap fixedMap)
        {
            var rawRequiredConcepts = typeParam.AllEffectiveInterfacesNoUseSiteDiagnostics;

            // The concepts from above are in terms of the method's type
            // parameters.  In order to be able to unify properly, we need to
            // substitute the inferences we've made so far.
            var rc = new ArrayBuilder<TypeSymbol>();
            foreach (var con in rawRequiredConcepts)
            {
                rc.Add(fixedMap.SubstituteType(con).AsTypeSymbolOnly());
            }

            var unused = new HashSet<DiagnosticInfo>();

            // Now we can do some optimisation: if we're asking for a concept,
            // we don't need to ask for its base concepts.
            var rc2 = new ArrayBuilder<TypeSymbol>();
            foreach (var c1 in rc)
            {
                var needed = true;
                foreach (var c2 in rc)
                {
                    if (c1.ImplementsInterface(c2, ref unused))
                    {
                        needed = false;
                        break;
                    }
                }
                if (needed) rc2.Add(c1);
            }

            rc.Free();
            return rc2.ToImmutableAndFree();
        }

        #endregion Main driver
        #region First pass

        /// <summary>
        /// Performs the first pass of concept witness type inference.
        /// <para>
        /// This pass filters down a list of all possible instances into a set
        /// of candidate instances, such that each candidate instance
        /// implements all of the concepts required by the parameter being
        /// inferred.
        /// </para>
        /// </summary>
        /// <param name="allInstances">
        /// The set of instances available for this witness.
        /// </param>
        /// <param name="requiredConcepts">
        /// The list of concepts required by the type parameter being inferred.
        /// </param>
        /// <param name="boundParams">
        /// A list of all type parameters that have been bound by the
        /// containing scope, and thus cannot be substituted by unification.
        /// </param>
        /// <returns>
        /// An array of candidate instances after the first pass.
        /// </returns>
        private ImmutableArray<TypeSymbol> TryInferConceptWitnessFirstPass(ImmutableArray<TypeSymbol> allInstances,
            ImmutableArray<TypeSymbol> requiredConcepts,
            ImmutableHashSet<TypeParameterSymbol> boundParams)
        {
            // First, collect all of the instances satisfying 1).
            var firstPassInstanceBuilder = new ArrayBuilder<TypeSymbol>();
            foreach (var instance in allInstances)
            {
                MutableTypeMap unifyingSubstitutions = new MutableTypeMap();
                if (AllRequiredConceptsProvided(requiredConcepts, instance, boundParams, ref unifyingSubstitutions))
                {
                    // The unification may have provided us with substitutions
                    // that were needed to make the provided concepts fit the
                    // required concepts.
                    //
                    // It may be that some of these substitutions also need to
                    // apply to the actual instance so it can satisfy #2.
                    var result = unifyingSubstitutions.SubstituteType(instance).AsTypeSymbolOnly();
                    firstPassInstanceBuilder.Add(result);
                }
            }
            return firstPassInstanceBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Checks whether a list of required concepts is implemented by a
        /// candidate instance modulo unifying substitutions.
        /// <para>
        /// We don't check yet that the instance itself is satisfiable, just that
        /// it will satisfy our concept list if it is.
        /// </para>
        /// </summary>
        /// <param name="requiredConcepts">
        /// The list of required concepts to implement.
        /// </param>
        /// <param name="instance">
        /// The candidate instance.
        /// </param>
        /// <param name="boundParams">
        /// A list of all type parameters that have been bound by the
        /// containing scope, and thus cannot be substituted by unification.
        /// </param>
        /// <param name="unifyingSubstitutions">
        /// A map of type substitutions, populated by this method, which are
        /// required in order to make the instance implement the concepts.
        /// </param>
        /// <returns>
        /// True if, and only if, the given instance implements the given list
        /// of concepts.
        /// </returns>
        private bool AllRequiredConceptsProvided(ImmutableArray<TypeSymbol> requiredConcepts,
                                                 TypeSymbol instance,
                                                 ImmutableHashSet<TypeParameterSymbol> boundParams,
                                                 ref MutableTypeMap unifyingSubstitutions)
        {
            var providedConcepts =
                ((instance as TypeParameterSymbol)?.AllEffectiveInterfacesNoUseSiteDiagnostics
                 ?? ((instance as NamedTypeSymbol)?.AllInterfacesNoUseSiteDiagnostics)
                 ?? ImmutableArray<NamedTypeSymbol>.Empty);

            foreach (var requiredConcept in requiredConcepts)
            {
                bool thisProvided = false;
                foreach (var providedConcept in providedConcepts)
                {
                    if (TypeUnification.CanUnify(providedConcept,
                            requiredConcept,
                            ref unifyingSubstitutions,
                            boundParams))
                    {
                        thisProvided = true;
                        break;
                    }
                }

                if (!thisProvided) return false;
            }

            // If we got here, all required concepts must have been provided.
            return true;
        }

        #endregion First pass
        #region Second pass

        /// <summary>
        /// Performs the second pass of concept witness type inference.
        /// <para>
        /// This pass tries to fix any witness parameters in each candidate
        /// instance, eliminating it if it either has unfixed non-witness
        /// parameters, or the witness parameters cannot be fixed.  To do this,
        /// it recursively begins inference on the missing witnesses.
        /// </para>
        /// </summary>
        /// <param name="candidateInstances">
        /// The set of candidate instances after the first pass.
        /// </param>
        /// <param name="allInstances">
        /// The original list of all instances, used for recursive calls.
        /// </param>
        /// <param name="boundParams">
        /// A list of all type parameters that have been bound by the
        /// containing scope, and thus cannot be substituted by unification.
        /// </param>
        /// <returns>
        /// An array of candidate instances after the first pass.
        /// </returns>
        private ImmutableArray<TypeSymbol> TryInferConceptWitnessSecondPass(ImmutableArray<TypeSymbol> candidateInstances,
            ImmutableArray<TypeSymbol> allInstances,
            ImmutableHashSet<TypeParameterSymbol> boundParams)
        {
            var secondPassInstanceBuilder = new ArrayBuilder<TypeSymbol>();
            foreach (var instance in candidateInstances)
            {
                // Assumption: no witness parameter can depend on any other
                // witness parameter, so we can do recursive inference in
                // one pass.
                var unfixedWitnessBuilder = new ArrayBuilder<TypeParameterSymbol>();
                if (!GetRecursiveUnfixedConceptWitnesses(instance, ref unfixedWitnessBuilder))
                {
                    // This instance has some unfixed non-witness type
                    // parameters.  We can't infer these, so give up on this
                    // candidate instance.
                    continue;
                }
                var unfixedWitnesses = unfixedWitnessBuilder.ToImmutableAndFree();

                var fixedInstance = instance;
                // If there were no unfixed witnesses, we don't need to bother
                // with recursive inference--there's nothing to infer!
                if (!unfixedWitnesses.IsEmpty)
                {
                    // Type parameters can't have unfixed witnesses.
                    Debug.Assert(instance.Kind == SymbolKind.NamedType);
                    var nt = (NamedTypeSymbol)instance;

                    // To do recursive inference, we need to supply the current
                    // fixed type parameters as a type map again.
                    var recurFixedMap = new MutableTypeMap();
                    var targs = nt.TypeArguments;
                    var tpars = nt.TypeParameters;
                    for (int i = 0; i < tpars.Length; i++)
                    {
                        if (tpars[i] != targs[i]) recurFixedMap.Add(tpars[i], new TypeWithModifiers(targs[i]));
                    }

                    // Now try to infer the unfixed witnesses, recursively.
                    // TODO: can this be flattened into an iterative process?
                    // It shouldn't be a massive performance or stack issue,
                    // but still...
                    var success = true;
                    var recurSubstMap = new MutableTypeMap();
                    foreach (var unfixed in unfixedWitnesses)
                    {
                        var maybeFixed = TryInferConceptWitness(unfixed, allInstances, recurFixedMap, boundParams);
                        if (maybeFixed == null)
                        {
                            success = false;
                            break;
                        }
                        recurSubstMap.Add(unfixed, new TypeWithModifiers(maybeFixed));
                    }

                    // We couldn't infer all of the witnesses.  By our
                    // assumption, we can't infer anything more on this
                    // instance, so we give up on it.
                    if (!success) continue;

                    // Else, we now have a map that should fix all of the
                    // remaining parameters.
                    fixedInstance = recurSubstMap.SubstituteType(instance).AsTypeSymbolOnly();
                }

                // If we got this far, the instance _should_ have no unfixed
                // parameters, and can now be considered as a candidate for
                // inference.
                secondPassInstanceBuilder.Add(fixedInstance);
            }
            return secondPassInstanceBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Tries to find all unfixed type parameters in a candidate instance,
        /// adds those which are witnesses to a list, and fails if any is not
        /// a witness.
        /// </summary>
        /// <param name="instance">
        /// The candidate instance to investigate.
        /// </param>
        /// <param name="unfixed">
        /// The list of unfixed witness parameters to populate.
        /// </param>
        /// <returns>
        /// True if we didn't see any unfixed non-witness type parameters,
        /// which is a blocker on accepting <paramref name="instance"/> as a
        /// witness; false otherwise.
        /// </returns>
        private bool GetRecursiveUnfixedConceptWitnesses(TypeSymbol instance, ref ArrayBuilder<TypeParameterSymbol> unfixed)
        {
            Debug.Assert(instance.Kind == SymbolKind.NamedType || instance.Kind == SymbolKind.TypeParameter);

            // Only named types (ie instance declarations) can contain
            // unresolved concept witnesses.
            if (instance.Kind != SymbolKind.NamedType) return true;

            var nt = (NamedTypeSymbol)instance;
            var targs = nt.TypeArguments;
            var tpars = nt.TypeParameters;
            for (int i = 0; i < tpars.Length; i++)
            {
                // If a type parameter is its own argument, we assume this
                // means it hasn't yet been fixed.
                if (tpars[i] == targs[i])
                {
                    if (tpars[i].IsConceptWitness)
                    {
                        unfixed.Add(tpars[i]);
                    }
                    else
                    {
                        // This is an unfixed non-witness, which kills off our
                        // attempt to use this instance completely.
                        return false;
                    }
                }
            }

            // If we got here, then we haven't seen any unfixed non-witnesses.
            return true;
        }

        #endregion Second pass
        #region Third pass

        /// <summary>
        /// Performs the third pass of concept witness type inference.
        /// <para>
        /// This pass tries to find a single instance in the candidate set that
        /// dominates all other instances, eg. its concept is a strict
        /// super-interface of all other candidate instances' concepts.
        /// </para>
        /// </summary>
        /// <param name="candidateInstances">
        /// The set of instances to narrow.
        /// </param>
        /// <param name="fixedMap">
        /// A map mapping fixed type parameters to their type arguments.
        /// </param>
        /// <returns>
        /// An array of candidate instances after the third pass.  If there is
        /// no dominating instance, this will be
        /// <paramref name="candidateInstances"/>.
        /// </returns>
        ImmutableArray<TypeSymbol> TryInferConceptWitnessThirdPass(ImmutableArray<TypeSymbol> candidateInstances, MutableTypeMap fixedMap)
        {
            var ignore = new HashSet<DiagnosticInfo>();

            // TODO: This can invariably be made more efficient.
            foreach (var instance in candidateInstances)
            {
                bool isDominator = true;

                foreach (var otherInstance in candidateInstances)
                {
                    if (otherInstance == instance) continue;

                    foreach (var iface in otherInstance.AllInterfacesNoUseSiteDiagnostics)
                    {
                        if (!iface.IsConcept) continue;
                        if (!instance.ImplementsInterface(iface, ref ignore))
                        {
                            isDominator = false;
                            break;
                        }
                    }

                    if (!isDominator) break;
                }

                if (isDominator)
                {
                    var arb = new ArrayBuilder<TypeSymbol>();
                    arb.Add(instance);
                    return arb.ToImmutableAndFree();
                }
            }

            // If we didn't find a dominator, we didn't shrink the list, so
            // we have to return the original unshrunk list here.
            return candidateInstances;
        }

        #endregion Third pass
    }
}
