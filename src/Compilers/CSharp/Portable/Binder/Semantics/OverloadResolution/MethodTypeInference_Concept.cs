using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

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
            Debug.Assert(!AllFixed(),
                "Concept witness inference is pointless if there is nothing to infer");

            // First, make sure every unfixed type parameter is a concept, and
            // that we know where they all are so we can infer them later.
            ImmutableArray<int> conceptIndices;
            if (!GetMethodUnfixedConceptWitnesses(out conceptIndices)) return false;

            Debug.Assert(!conceptIndices.IsEmpty,
                "Tried to proceed with concept inference with no concept witnesses to infer");

            // We'll be checking to see if concepts defined on the missing
            // witness type parameters are implemented.  Since this means we
            // are checking something on the method definition, but need it
            // in terms of our fixed type arguments, we must make a mapping
            // from parameters to arguments.
            var fixedMap = this.MakeMethodFixedMap();

            var inferrer = ConceptWitnessInferrer.ForBinder(binder);
            bool success = true;

            // z

            foreach (int j in conceptIndices)
            {
                var maybeFixed = inferrer.Infer(_methodTypeParameters[j], fixedMap);
                if (maybeFixed == null) return false;
                Debug.Assert(maybeFixed.IsInstanceType() || maybeFixed.IsConceptWitness,
                    "Concept witness inference returned something other than a concept instance or witness");
                _fixedResults[j] = maybeFixed;
            }

            return success;
        }

        /// <summary>
        /// Checks that every unfixed type parameter is a concept witness, and
        /// stores their indices into an array.
        /// </summary>
        /// <param name="indices">
        /// The outgoing array of unfixed concept witnesses.
        /// </param>
        /// <returns>
        /// True if, and only if, every unfixed type parameter is a concept
        /// witness.
        /// </returns>
        private bool GetMethodUnfixedConceptWitnesses(out ImmutableArray<int> indices)
        {
            var iBuilder = new ArrayBuilder<int>();

            for (int i = 0; i < _methodTypeParameters.Length; i++)
            {
                if (IsUnfixed(i))
                {
                    if (!_methodTypeParameters[i].IsConceptWitness)
                    {
                        iBuilder.Free();
                        return false;
                    }
                    iBuilder.Add(i);
                }
            }

            indices = iBuilder.ToImmutableAndFree();
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
                if (!IsUnfixed(i)) mt.Add(_methodTypeParameters[i], new TypeWithModifiers(_fixedResults[i]));
            }

            return mt;
        }
    }

    /// <summary>
    /// An object that, given a series of viable instances and bound type
    /// parameters, can perform concept witness inference.
    /// </summary>
    internal class ConceptWitnessInferrer
    {
        /// <summary>
        /// The list of all instances in scope for this inferrer.
        /// These can be either type parameters (eg. witnesses passed in
        /// through constraints at the method or class level) or named
        /// types (instance declarations).
        /// </summary>
        private readonly ImmutableArray<TypeSymbol> _allInstances;

        /// <summary>
        /// The set of all type parameters in scope that are bound:
        /// we cannot substitute for them in unification.  Usually this is
        /// the set of type parameters introduced through type parameter
        /// lists on methods and classes in scope.
        /// </summary>
        private readonly ImmutableHashSet<TypeParameterSymbol> _boundParams;

        /// <summary>
        /// Constructs a new ConceptWitnessInferrer.
        /// </summary>
        /// <param name="allInstances">
        /// The list of all instances in scope for this inferrer.
        /// </param>
        /// <param name="boundParams">
        /// The set of all type parameters in scope that are bound, and
        /// cannot be substituted out in unification.
        /// </param>
        public ConceptWitnessInferrer(ImmutableArray<TypeSymbol> allInstances, ImmutableHashSet<TypeParameterSymbol> boundParams)
        {
            _allInstances = allInstances;
            _boundParams = boundParams;
        }

        #region Setup from binder

        /// <summary>
        /// Constructs a new ConceptWitnessInferrer taking its instance pool
        /// and bound parameter set from a given binder.
        /// </summary>
        /// <param name="binder">
        /// The binder providing scope for the new inferrer.
        /// </param>
        /// <returns>
        /// An inferrer that will consider all instances in scope at the given
        /// binder, and refuse to unify on any type parameters bound in methods
        /// or classes in the binder's vicinity.
        /// </returns>
        public static ConceptWitnessInferrer ForBinder(Binder binder)
        {
            // We need two things from the outer scope:
            // 1) All instances visible to this method call;
            // 2) All type parameters bound in the method and class.
            // For efficiency, we do these in one go.
            // TODO: Ideally this should be cached at some point, perhaps on the
            // compilation or binder.
            ImmutableArray<TypeSymbol> allInstances;
            ImmutableHashSet<TypeParameterSymbol> boundParams;
            SearchScopeForInstancesAndParams(binder, out allInstances, out boundParams);
            return new ConceptWitnessInferrer(allInstances, boundParams);
        }

        /// <summary>
        /// Traverses the scope induced by the given binder for visible
        /// instances and fixed type parameters
        /// </summary>
        /// <param name="binder">
        /// The binder providing scope for this query.
        /// </param>
        /// <param name="allInstances">
        /// An immutable array of symbols (either type parameters or named
        /// types) representing concept instances available in the scope
        /// of <paramref name="binder"/>.
        /// </param>
        /// <param name="fixedParams">
        /// An immutable array of symbols (either type parameters or named
        /// types) representing concept instances available in the scope
        /// of <paramref name="binder"/>.
        /// </param>
        private static void SearchScopeForInstancesAndParams(Binder binder, out ImmutableArray<TypeSymbol> allInstances, out ImmutableHashSet<TypeParameterSymbol> fixedParams)
        {
            var iBuilder = new ArrayBuilder<TypeSymbol>();
            var fpBuilder = new ArrayBuilder<TypeParameterSymbol>();

            var ignore = new HashSet<DiagnosticInfo>();

            for (var b = binder; b != null; b = b.Next)
            {
                b.GetConceptInstances(false, iBuilder, binder, ref ignore);
                b.GetFixedTypeParameters(fpBuilder);
            }

            iBuilder.RemoveDuplicates();
            allInstances = iBuilder.ToImmutableAndFree();
            fixedParams = fpBuilder.ToImmutableHashSet();
            fpBuilder.Free();
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
        /// <param name="fixedParams">
        /// The set to populate with fixed type parameters.
        /// </param>
        private static void SearchContainerForInstancesAndParams(Symbol container,
            ref ArrayBuilder<TypeSymbol> instances,
            ref HashSet<TypeParameterSymbol> fixedParams)
        {
            // Only methods and named types have constrained witnesses.
            if (container.Kind != SymbolKind.Method && container.Kind != SymbolKind.NamedType) return;

            ImmutableArray<TypeParameterSymbol> tps = GetTypeParametersOf(container);

            foreach (var tp in tps)
            {
                if (tp.IsConceptWitness) instances.Add(tp);
                fixedParams.Add(tp);
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
        private static void GetNamedInstances(Binder binder, Symbol container, ref ArrayBuilder<TypeSymbol> instances)
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

        #endregion Setup from binder
        #region Main driver

        /// <summary>
        /// Tries to infer a batch of concept witnesses.
        /// </summary>
        /// <param name="witnesses">
        /// The array of concept witnesses to fix.
        /// </param>
        /// <param name="fixedMap">
        /// The map from all of the fixed, non-witness type parameters in the
        /// same type parameter list as <paramref name="witnesses"/>
        /// to their arguments.
        /// </param>
        /// <param name="chain">
        /// The set of instances we've passed through recursively to get here,
        /// used to abort recursive calls if they will create cycles.
        /// </param>
        /// <returns>
        /// An array of fixed concept witnesses.  If any is null, then
        /// inference has failed.
        /// </returns>
        internal ImmutableArray<TypeSymbol> InferMany(
            ImmutableArray<TypeParameterSymbol> witnesses,
            // TODO: associated parameters
            MutableTypeMap fixedMap,
            ImmutableHashSet<NamedTypeSymbol> chain = null
        )
        {
            var resultsBuilder = ArrayBuilder<TypeSymbol>.GetInstance();

            foreach (var witness in witnesses)
            {
                // TODO: create type map
                // TODO: backpropagate to associated parameters
                resultsBuilder.Add(witness);
            }

            return resultsBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Tries to infer a suitable instance for the given concept witness
        /// type parameter.
        /// </summary>
        /// <param name="typeParam">
        /// The type parameter that is the concept witness to infer.  This
        /// must actually be a concept witness.
        /// </param>
        /// <param name="fixedMap">
        /// The map from all of the fixed, non-witness type parameters in the
        /// same type parameter list as <paramref name="typeParam"/>
        /// to their arguments.
        /// </param>
        /// <param name="chain">
        /// The set of instances we've passed through recursively to get here,
        /// used to abort recursive calls if they will create cycles.
        /// </param>
        /// <returns>
        /// Null if inference failed; else, the inferred concept instance.
        /// </returns>
        internal TypeSymbol Infer(TypeParameterSymbol typeParam, MutableTypeMap fixedMap, ImmutableHashSet<NamedTypeSymbol> chain = null)
        {
            if (chain == null) chain = ImmutableHashSet<NamedTypeSymbol>.Empty;

            Debug.Assert(typeParam.IsConceptWitness,
                "Tried to do concept witness inference on a non-concept-witness type parameter");

            // From here, we can only decrease the number of considered
            // instances, so we can't assign an instance to a witness
            // parameter if there aren't any to begin with.
            if (_allInstances.IsEmpty) return null;

            // @t-mawind
            // An instance satisfies inference if:
            //
            // 1) for all concepts required by the type parameter, at least
            //    one concept implemented by the instances unifies with that
            //    concept without capturing bound type parameters;
            // 2) all of the type parameters of that instance can be bound,
            //    both by the substitutions from the unification above and also
            //    by recursively trying to infer any missing concept witnesses.
            //
            // The first part is equivalent to establishing
            //    witness :- instance.
            //
            // The second part is equivalent to resolving
            //    instance :- dependency1; dependency2; ...
            // by trying to establish the dependencies as separate queries.
            //
            // After the second part, if we have multiple possible instances,
            // we try to see if one implements a subconcept of all of the other
            // instances.  If so, we narrow to that specific instance.
            //
            // If we have multiple satisfying instances, or zero, we fail.

            // TODO: We don't yet have #2, so we presume that if we have any
            // concept-witness type parameters we've failed.

            var requiredConcepts = GetRequiredConceptsFor(typeParam, fixedMap);
            // This might happen if, for example, someone explicitly annotates
            // a parameter as [ConceptWitness] but doesn't put any constraints
            // on it.  We don't infer in this case, because any and every
            // possible instance will match.
            if (requiredConcepts.IsEmpty) return null;

            var firstPassInstances = AllInstancesSatisfyingGoal(requiredConcepts);
            Debug.Assert(firstPassInstances.Length <= _allInstances.Length,
                "First pass of concept witness inference should not grow the instance list");
            // We can't infer if none of the instances implement our concept!
            // However, if we have more than one candidate instance at this
            // point, we shouldn't bail until we've made sure only one of them
            // passes 2).
            if (firstPassInstances.IsEmpty) return null;

            var secondPassInstances = ToSatisfiableInstances(firstPassInstances, chain);
            Debug.Assert(secondPassInstances.Length <= firstPassInstances.Length,
                "Second pass of concept witness inference should not grow the instance list");

            // We only do tie breaking in the case of actual ties.
            var thirdPassInstances = secondPassInstances;
            if (1 < secondPassInstances.Length) thirdPassInstances = TieBreakInstances(secondPassInstances);
            Debug.Assert(thirdPassInstances.Length <= secondPassInstances.Length,
                "Third pass of concept witness inference should not grow the instance list");

            // Either ambiguity, or an outright lack of inference success.
            if (thirdPassInstances.Length != 1) return null;
            Debug.Assert(thirdPassInstances[0] != null,
                "Inference claims to have succeeded, but has returned a null instance");
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
        private static ImmutableArray<TypeSymbol> GetRequiredConceptsFor(TypeParameterSymbol typeParam, MutableTypeMap fixedMap)
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
            // This is analogous to Haskell context reduction, but somewhat
            // simpler: because of the way our concepts are architected, much
            // of what Haskell does makes no sense.
            var rc2 = new ArrayBuilder<TypeSymbol>();
            foreach (var c1 in rc)
            {
                var needed = true;
                foreach (var c2 in rc)
                {
                    if (c2.ImplementsInterface(c1, ref unused))
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

        /// <summary>
        /// Gets the type parameters of an arbitrary symbol.
        /// </summary>
        /// <param name="symbol">
        /// The symbol for which we are getting type parameters.
        /// </param>
        /// <returns>
        /// If the symbol is a generic method or named type, its parameters;
        /// else, the empty list.
        /// </returns>
        internal static ImmutableArray<TypeParameterSymbol> GetTypeParametersOf(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                return ((MethodSymbol)symbol).TypeParameters;
                case SymbolKind.NamedType:
                return ((NamedTypeSymbol)symbol).TypeParameters;
                default:
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
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
        /// <param name="requiredConcepts">
        /// The list of concepts required by the type parameter being inferred.
        /// </param>
        /// <returns>
        /// An array of candidate instances after the first pass.
        /// </returns>
        private ImmutableArray<TypeSymbol> AllInstancesSatisfyingGoal(ImmutableArray<TypeSymbol> requiredConcepts)
        {
            Debug.Assert(!requiredConcepts.IsEmpty,
                "First pass of inference is pointless when there are no required concepts");
            Debug.Assert(!_allInstances.IsEmpty,
                "First pass of inference is pointless when there are no available instances");

            // First, collect all of the instances satisfying 1).
            var firstPassInstanceBuilder = new ArrayBuilder<TypeSymbol>();
            foreach (var instance in _allInstances)
            {
                MutableTypeMap unifyingSubstitutions;
                if (AllRequiredConceptsProvided(requiredConcepts, instance, out unifyingSubstitutions))
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
        /// The list of required concepts to implement.  Must be non-empty.
        /// </param>
        /// <param name="instance">
        /// The candidate instance.
        /// </param>
        /// <param name="unifyingSubstitutions">
        /// A map of type substitutions, populated by this method, which are
        /// required in order to make the instance implement the concepts.
        /// </param>
        /// <returns>
        /// True if, and only if, the given instance implements the given list
        /// of concepts.
        /// </returns>
        private bool AllRequiredConceptsProvided(ImmutableArray<TypeSymbol> requiredConcepts, TypeSymbol instance, out MutableTypeMap unifyingSubstitutions)
        {
            Debug.Assert(!requiredConcepts.IsEmpty,
                "Checking that all required concepts are provided is pointless when there are none");

            unifyingSubstitutions = new MutableTypeMap();

            var providedConcepts =
                ((instance as TypeParameterSymbol)?.AllEffectiveInterfacesNoUseSiteDiagnostics
                 ?? ((instance as NamedTypeSymbol)?.AllInterfacesNoUseSiteDiagnostics)
                 ?? ImmutableArray<NamedTypeSymbol>.Empty);
            if (providedConcepts.IsEmpty) return false;

            foreach (var requiredConcept in requiredConcepts)
            {
                if (!IsRequiredConceptProvided(requiredConcept, providedConcepts, ref unifyingSubstitutions)) return false;
            }

            // If we got here, all required concepts must have been provided.
            return true;
        }

        /// <summary>
        /// Checks whether a single required concept is implemented by a
        /// set of provided concepts modulo unifying substitutions.
        /// <para>
        /// We don't check yet that the instance itself is satisfiable, just that
        /// it will satisfy our concept list if it is.
        /// </para>
        /// </summary>
        /// <param name="requiredConcept">
        /// The required concept to implement.
        /// </param>
        /// <param name="providedConcepts">
        /// The provided concepts to check against.  Must be non-empty.
        /// </param>
        /// <param name="unifyingSubstitutions">
        /// A map of type substitutions, added to by this method, which are
        /// required in order to make the instance implement the concepts.
        /// Any existing substitutions in this map, for example those fixed
        /// by previous required concepts, are applied during unification.
        /// </param>
        /// <returns>
        /// True if, and only if, the given set of provided concepts implement
        /// the given list of concepts.
        /// </returns>
        private bool IsRequiredConceptProvided(TypeSymbol requiredConcept, ImmutableArray<NamedTypeSymbol> providedConcepts, ref MutableTypeMap unifyingSubstitutions)
        {
            Debug.Assert(!providedConcepts.IsEmpty,
                "Checking for provision of concept is pointless when no concepts are provided");

            foreach (var providedConcept in providedConcepts)
            {
                if (TypeUnification.CanUnify(providedConcept, requiredConcept, ref unifyingSubstitutions, _boundParams)) return true;
            }

            return false;
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
        /// <param name="chain">
        /// The set of instances we've passed through recursively to get here,
        /// used to abort recursive calls if they will create cycles.
        /// </param>
        /// <returns>
        /// An array of candidate instances after the first pass.
        /// </returns>
        private ImmutableArray<TypeSymbol> ToSatisfiableInstances(ImmutableArray<TypeSymbol> candidateInstances, ImmutableHashSet<NamedTypeSymbol> chain)
        {
            // Remember: even if we have one instance left here, it could be
            // unsatisfiable, so we have to run this pass on it.
            Debug.Assert(!candidateInstances.IsEmpty,
                "Performing second pass of witness inference is pointless when we have no candidates left");

            var secondPassInstanceBuilder = new ArrayBuilder<TypeSymbol>();
            foreach (var instance in candidateInstances)
            {
                // Assumption: no witness parameter can depend on any other
                // witness parameter, so we can do recursive inference in
                // one pass.
                ImmutableArray<TypeParameterSymbol> unfixedWitnesses;
                if (!GetRecursiveUnfixedConceptWitnesses(instance, out unfixedWitnesses))
                {
                    // This instance has some unfixed non-witness type
                    // parameters.  We can't infer these, so give up on this
                    // candidate instance.
                    continue;
                }

                var fixedInstance = instance;
                // If there were no unfixed witnesses, we don't need to bother
                // with recursive inference--there's nothing to infer!
                if (!unfixedWitnesses.IsEmpty)
                {
                    Debug.Assert(instance.Kind == SymbolKind.NamedType,
                        "Tried to do recursive inference on an instance type that cannot have unfixed witnesses");
                    var nt = (NamedTypeSymbol)instance;

                    // Do cycle detection: have we already set up a recursive
                    // call for this instance with these type parameters?
                    if (chain.Contains(nt)) continue;
                    var newChain = chain.Add(nt);

                    // If this call fails, we couldn't infer all of the
                    // witnesses.  By our assumption, we can't infer anything
                    // more on this instance, so we give up on it.
                    MutableTypeMap recurSubstMap;
                    if (!InferRecursively(nt, unfixedWitnesses, newChain, out recurSubstMap)) continue;

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
        /// Prepares a recursive inference round on an instance.
        /// </summary>
        /// <param name="instance">
        /// The instance whose missing witnesses are to be inferred.
        /// </param>
        /// <param name="unfixedWitnesses">
        /// The set of unfixed witness parameters to infer.
        /// </param>
        /// <param name="chain">
        /// The set of instances we've passed through recursively to get here,
        /// used to abort the recursive call if it will create a cycle.
        /// </param>
        /// <param name="recurSubstMap">
        /// The map of witness-fixing substitutions to return on success.
        /// </param>
        /// <returns>
        /// True if, and only if, we were able to infer every unfixed witness
        /// for this instance without generating cycles.
        /// </returns>
        private bool InferRecursively(NamedTypeSymbol instance, ImmutableArray<TypeParameterSymbol> unfixedWitnesses, ImmutableHashSet<NamedTypeSymbol> chain, out MutableTypeMap recurSubstMap)
        {
            Debug.Assert(chain.Contains(instance),
                "Current instance has not been put in the cycle detection chain before recursively inferring");

            // In recursive inference, the set of known type argument
            // substitutions is those we made when fixing this instance.
            // We thus need to re-make the fixedMap.
            var recurFixedMap = new MutableTypeMap();
            var targs = instance.TypeArguments;
            var tpars = instance.TypeParameters;
            for (int i = 0; i < tpars.Length; i++)
            {
                if (tpars[i] != targs[i]) recurFixedMap.Add(tpars[i], new TypeWithModifiers(targs[i]));
            }

            // Now try to infer the unfixed witnesses, recursively.
            // TODO: can this be flattened into an iterative process?
            // It shouldn't be a massive performance or stack issue,
            // but still...
            recurSubstMap = new MutableTypeMap();
            var maybeFixed = InferMany(unfixedWitnesses, recurFixedMap, chain);
            for (int i = 0; i < maybeFixed.Length; i++)
            {
                if (maybeFixed[i] == null) return false;
                recurSubstMap.Add(unfixedWitnesses[i], new TypeWithModifiers(maybeFixed[i]));
            }

            return true;
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
        private static bool GetRecursiveUnfixedConceptWitnesses(TypeSymbol instance, out ImmutableArray<TypeParameterSymbol> unfixed)
        {
            Debug.Assert(instance.Kind == SymbolKind.NamedType || instance.Kind == SymbolKind.TypeParameter,
                "Tried to infer recursively on an incorrect instance type");

            var uBuilder = new ArrayBuilder<TypeParameterSymbol>();

            // Only named types (ie instance declarations) can contain
            // unresolved concept witnesses.
            if (instance.Kind != SymbolKind.NamedType)
            {
                unfixed = uBuilder.ToImmutableAndFree();
                return true;
            }
            var nt = (NamedTypeSymbol)instance;

            var targs = nt.TypeArguments;
            var tpars = nt.TypeParameters;
            Debug.Assert(targs.Length == tpars.Length,
                "Type parameter and argument arrays are out of sync");
            for (int i = 0; i < tpars.Length; i++)
            {
                // If a type parameter is its own argument, we assume this
                // means it hasn't yet been fixed.
                if (tpars[i] == targs[i])
                {
                    if (!tpars[i].IsConceptWitness)
                    {
                        // This is an unfixed non-witness, which kills off our
                        // attempt to use this instance completely.
                        unfixed = ImmutableArray<TypeParameterSymbol>.Empty;
                        uBuilder.Free();
                        return false;
                    }

                    // Otherwise, it must be recorded as an unfixed witness.
                    uBuilder.Add(tpars[i]);
                }
            }

            // If we got here, then we haven't seen any unfixed non-witnesses.
            unfixed = uBuilder.ToImmutableAndFree();
            return true;
        }

        #endregion Second pass
        #region Third pass

        /// <summary>
        /// Performs the third pass of concept witness type inference.
        /// <para>
        /// This pass tries to find a single instance in the candidate set that
        /// is 'better' than all other instances, eg. its concept is a strict
        /// super-interface of all other candidate instances' concepts.
        /// </para>
        /// </summary>
        /// <param name="candidateInstances">
        /// The set of instances to narrow.
        /// </param>
        /// <returns>
        /// An array of candidate instances after the third pass.
        /// </returns>
        private static ImmutableArray<TypeSymbol> TieBreakInstances(ImmutableArray<TypeSymbol> candidateInstances)
        {
            Debug.Assert(1 < candidateInstances.Length,
                "Tie-breaking is pointless if we have zero or one instances");

            // We now perform an array of 'better concept witness' checks to
            // try to narrow the list of instances to zero or one.

            var mostSpecificConceptInstances = FilterToMostSpecificConceptInstances(candidateInstances);
            Debug.Assert(mostSpecificConceptInstances.Length <= candidateInstances.Length,
                "Filtering to most-specific-concept instances should not grow the instance list");
            if (mostSpecificConceptInstances.Length <= 1) return mostSpecificConceptInstances;

            var mostSpecificParamInstances = FilterToMostSpecificParamInstances(mostSpecificConceptInstances);
            Debug.Assert(mostSpecificParamInstances.Length <= mostSpecificConceptInstances.Length,
                "Filtering to most-specific-param instances should not grow the instance list");

            return mostSpecificParamInstances;
        }

        /// <summary>
        /// Filters a set of candidate instances to those that implement at
        /// least all of the concepts of every other candidate instance.
        /// </summary>
        /// <param name="candidateInstances">
        /// The set of instances to filter.
        /// </param>
        /// <returns>
        /// <paramref name="candidateInstances"/>, filtered to contain only
        /// those instances that implement every concept of every other
        /// instance in <paramref name="candidateInstances"/>.
        /// </returns>
        private static ImmutableArray<TypeSymbol> FilterToMostSpecificConceptInstances(ImmutableArray<TypeSymbol> candidateInstances)
        {
            Debug.Assert(1 < candidateInstances.Length,
                "Filtering to most-specific-concept instances is pointless if we have zero or one instances");

            var arb = new ArrayBuilder<TypeSymbol>();

            // TODO: This can invariably be made more efficient.
            foreach (var instance in candidateInstances)
            {
                if (ImplementsConceptsOfOtherInstances(instance, candidateInstances)) arb.Add(instance);
                // Note that this will only break ties if one instance
                // implements effectively sub-concepts of all other
                // instances: if two instances implement precisely the
                // same concept set, both will be added to arb and the
                // check will fail.
            }

            return arb.ToImmutableAndFree();
        }

        /// <summary>
        /// Checks whether one instance implements all of the concepts, either
        /// directly or through sub-concepts, of a set of other concepts.
        /// </summary>
        /// <param name="instance">
        /// The instance to compare.
        /// </param>
        /// <param name="otherInstances">
        /// A list of other instances to which this instance should be compared.
        /// This may include <paramref name="instance"/>, in which case it will
        /// be ignored.
        /// </param>
        /// <returns>
        /// True if, and only if, <paramref name="instance"/> implements all of
        /// the concepts of instances in <paramref name="otherInstances"/>.
        /// </returns>
        private static bool ImplementsConceptsOfOtherInstances(TypeSymbol instance, ImmutableArray<TypeSymbol> otherInstances)
        {
            Debug.Assert(!otherInstances.IsEmpty,
                "Trying to check whether an instance implements concepts of zero other instances is pointless");

            var ignore = new HashSet<DiagnosticInfo>();

            foreach (var otherInstance in otherInstances)
            {
                if (otherInstance == instance) continue;

                foreach (var iface in otherInstance.AllInterfacesNoUseSiteDiagnostics)
                {
                    if (!iface.IsConcept) continue;
                    if (!instance.ImplementsInterface(iface, ref ignore)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Filters a set of candidate instances to those whose type parameters
        /// are no less specific than those of any other instance.
        /// <para>
        /// If any instance is more specific than the others, then the others
        /// are less specific and removed, returning the one 'best' instance.
        /// </para>
        /// <para>
        /// Currently, we only rule that one instance is more specific than the
        /// other if it has non-witness type parameters whereas the other does
        /// not.  This is probably overly conservative, however.
        /// </para>
        /// </summary>
        /// <param name="candidateInstances">
        /// The set of instances to filter.
        /// </param>
        /// <returns>
        /// <paramref name="candidateInstances"/>, filtered to contain only
        /// those instances whose type parameters are more specific than those
        /// of any other instance in <paramref name="candidateInstances"/>.
        /// </returns>
        private static ImmutableArray<TypeSymbol> FilterToMostSpecificParamInstances(ImmutableArray<TypeSymbol> candidateInstances)
        {
            Debug.Assert(1 < candidateInstances.Length,
                "Filtering to most-specific-param instances is pointless if we have zero or one instances");

            var arb = new ArrayBuilder<TypeSymbol>();

            // TODO: This can invariably be made more efficient.
            foreach (var instance in candidateInstances)
            {
                if (!ParamsLessSpecific(instance, candidateInstances)) arb.Add(instance);
            }

            return arb.ToImmutableAndFree();
        }

        /// <summary>
        /// Decides whether an instance is strictly less specific than at least
        /// one other instance.
        /// </summary>
        /// <param name="instance">
        /// The instance to compare.
        /// </param>
        /// <param name="otherInstances">
        /// A list of other instances to which this instance should be compared.
        /// This may include <paramref name="instance"/>, in which case it will
        /// be ignored.
        /// </param>
        /// <returns>
        /// True if, and only if, <paramref name="instance"/> is strictly less
        /// specific than any of the other instances in
        /// <paramref name="otherInstances"/>.
        /// </returns>
        private static bool ParamsLessSpecific(TypeSymbol instance, ImmutableArray<TypeSymbol> otherInstances)
        {
            Debug.Assert(!otherInstances.IsEmpty,
                "Trying to check whether an instance has less specific params than zero other instances is pointless");

            // Currently, we do a very basic check based on non-witness type
            // parameter counts.  This could be much more sophisticated.

            bool instanceHasNonWitnesses = false;
            foreach (var typeParam in GetTypeParametersOf(instance))
            {
                if (!typeParam.IsConceptWitness)
                {
                    instanceHasNonWitnesses = true;
                    break;
                }
            }

            // No need to do the below check if we don't have non-witness type
            // params: the only way something can be more specific than us at
            // the moment is if we weren't.
            // This will need to go if we do something more sophisticated.
            if (!instanceHasNonWitnesses) return false;

            foreach (var otherInstance in otherInstances)
            {
                if (instance == otherInstance) continue;

                // TODO: cache this per instance?
                bool otherHasNonWitnesses = false;
                foreach (var typeParam in GetTypeParametersOf(otherInstance))
                {
                    if (!typeParam.IsConceptWitness)
                    {
                        otherHasNonWitnesses = true;
                        break;
                    }
                }

                // An instance is more specific if it has no non-witness type
                // parameters, but the other instance does.  Flip this logic to
                // get an early less-specific result.
                if (instanceHasNonWitnesses && !otherHasNonWitnesses) return true;
            }

            return false;
        }

        #endregion Third pass
        #region Part-inference

        // Part-inference is when we are given a set of type arguments for a
        // generic named type or method with more than one concept witness,
        // but the type argument list is seemingly missing those witnesses.
        // In this case, we use the concept type inferrer to fill in the
        // omitted witnesses.

        public ImmutableArray<TypeSymbol> PartInfer(ImmutableArray<TypeSymbol> typeArguments, ImmutableArray<TypeParameterSymbol> typeParameters)
        {
            Debug.Assert(typeArguments.Length < typeParameters.Length,
                "Part-inference is pointless if we already have all the type parameters");

            var allArgumentsBuilder = ArrayBuilder<TypeSymbol>.GetInstance();

            // Assume that the missing type arguments are concept
            // witnesses, and extend the given type arguments
            // with them.
            //
            // To infer the missing arguments, we need a full
            // map from non-concept type parameters to the type
            // arguments we _do_ have.  We can do this at the
            // same time as extending the arguments by
            // initially supplying placeholders and inferring
            // them later.
            var missingIndices = ArrayBuilder<int>.GetInstance();
            var fixedMap = new MutableTypeMap();
            int j = 0;
            for (int i = 0; i < typeParameters.Length; i++)
            {
                if (typeParameters[i].IsConceptWitness)
                {
                    allArgumentsBuilder.Add(typeParameters[i]);
                    missingIndices.Add(i); // Come back to this later.
                }
                else
                {
                    allArgumentsBuilder.Add(typeArguments[j]);
                    fixedMap.Add(typeParameters[i], new TypeWithModifiers(typeArguments[i]));
                    j++;
                }
            }

            // Now we can do the inference step.
            // We assume the given arguments are correct, so
            // don't bother doing any inference other than that
            // for witnesses.
            foreach (int k in missingIndices)
            {
                var inferred = Infer(typeParameters[k], fixedMap);
                // TODO: more specific error?
                if (inferred == null)
                {
                    allArgumentsBuilder.Free();
                    return ImmutableArray<TypeSymbol>.Empty;
                }
                allArgumentsBuilder[k] = inferred;
            }

            return allArgumentsBuilder.ToImmutableAndFree();
        }

        #endregion Part-inference
    }
}
