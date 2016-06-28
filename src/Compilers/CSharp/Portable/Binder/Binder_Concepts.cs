// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// Synthesizes the correct receiver of a witness invocation.
        /// </summary>
        /// <param name="syntax">
        /// The syntax from which the receiver is being synthesized.
        /// </param>
        /// <param name="witness">
        /// The witness on which we are invoking a method.
        /// </param>
        /// <returns></returns>
        BoundExpression SynthesizeWitnessInvocationReceiver(CSharpSyntaxNode syntax, TypeSymbol witness)
        {
            return new BoundDefaultOperator(syntax, witness) { WasCompilerGenerated = true };
        }

        /// <summary>
        /// Retrieves the list of witnesses available in this particular
        /// binder's scope.
        /// </summary>
        /// <param name="onlyExplicitWitnesses">
        /// If true, only return witnesses that have been explicitly put
        /// into scope by a concept constraint.
        /// </param>
        /// <param name="instances">
        /// The array builder to populate with instances.
        /// </param>
        /// <param name="originalBinder">
        /// The call-site binder.
        /// </param>
        /// <param name="useSiteDiagnostics">
        /// Diagnostics set at the use-site.
        /// </param>
        internal virtual void GetConceptInstances(bool onlyExplicitWitnesses, ArrayBuilder<TypeSymbol> instances, Binder originalBinder, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // By default, binders have no instances.
            return;
        }

        /// <summary>
        /// Retrieves the type parameters fixed by parameter lists in
        /// this particular binder's scope.
        /// </summary>
        /// <param name="fixedTypeParams">
        /// The array builder to populate with type parameters.
        /// </param>
        internal virtual void GetFixedTypeParameters(ArrayBuilder<TypeParameterSymbol> fixedTypeParams)
        {
            // By default, binders have no fixed type parameters.
            return;
        }

        internal virtual void LookupConceptMethodsInSingleBinder(LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var instanceBuilder = ArrayBuilder<TypeSymbol>.GetInstance();
            GetConceptInstances(true, instanceBuilder, originalBinder, ref useSiteDiagnostics);
            var instances = instanceBuilder.ToImmutableAndFree();
            foreach (var instance in instances)
            {
                // Currently only explicit witnesses, ie type parameters, may
                // be probed for concept methods.
                var tpInstance = instance as TypeParameterSymbol;
                if (tpInstance == null) continue;
                LookupSymbolsInWitness(tpInstance, result, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
            }
        }

        /// <summary>
        /// Tries to look up symbols inside a witness type parameter.
        /// <para>
        /// This lookup checks all of the concepts this witness implements
        /// to see if any contain a viable method matching the symbol.
        /// </para>
        /// <para>
        /// This lookup approach only works for methods, and returns a
        /// method whose parent is a type parameter.  We rely on later
        /// binder stages to detect this and resolve it back to a proper
        /// statement.
        /// </para>
        /// </summary>
        /// <param name="witness">
        /// The type witness into which we are looking.
        /// </param>
        /// <param name="result">
        /// The lookup result to populate.
        /// </param>
        /// <param name="name">
        /// The name of the member being looked-up.
        /// </param>
        /// <param name="arity">
        /// The arity of the member being looked up.
        /// </param>
        /// <param name="basesBeingResolved">
        /// The set of bases being resolved.
        /// </param>
        /// <param name="options">
        /// The lookup options in effect.
        /// </param>
        /// <param name="originalBinder">
        /// The top-level binder.
        /// </param>
        /// <param name="diagnose">
        /// Whether or not we are diagnosing.
        /// </param>
        /// <param name="useSiteDiagnostics">
        /// Diagnostics set at the use-site.
        /// </param>
        internal void LookupSymbolsInWitness(
        TypeParameterSymbol witness, LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(witness.IsConceptWitness);

            // Concepts are just interfaces, so we look at every possible
            // interface this witness has been constrained to implement.
            foreach (var iface in witness.AllEffectiveInterfacesNoUseSiteDiagnostics)
            {
                if (!iface.IsConcept) continue;

                // We're assuming that the above handles inheritance for us.
                // This may be a mistake.
                var members = GetCandidateMembers(iface, name, options, originalBinder);
                foreach (var member in members)
                {
                    // Don't bother trying to resolve non-methods:
                    // concepts can't have them, and we only have shims for
                    // dealing with methods later on anyway.
                    if (member.Kind != SymbolKind.Method) continue;
                    var method = member as MethodSymbol;
                    Debug.Assert(method != null);

                    // Suppose our witness is W : C<A>, and this finds C<A>.M(x).
                    // We need to return that we found W.M(x), but W is a type
                    // parameter!  While we can handle this later on in binding,
                    // the main issue is changing C<A> to W, for which we use
                    // a synthesized method symbol.
                    var witnessMethod = new SynthesizedWitnessMethodSymbol(method, witness);
                    SingleLookupResult resultOfThisMember = originalBinder.CheckViability(witnessMethod, arity, options, witness, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                    result.MergeEqual(resultOfThisMember);
                }
            }
        }

        private MethodGroupResolution ResolvePossibleConceptMethod(
            BoundMethodGroup methodGroup,
            CSharpSyntaxNode expression,
            string methodName,
            AnalyzedArguments analyzedArguments,
            bool isMethodGroupConversion)
        {
            var firstResult = new MethodGroupResolution();
            int arity;
            LookupOptions options;

            var typeArguments = methodGroup.TypeArgumentsOpt;
            if (typeArguments.IsDefault)
            {
                arity = 0;
                options = LookupOptions.AllMethodsOnArityZero;
            }
            else
            {
                arity = typeArguments.Length;
                options = LookupOptions.Default;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            // TODO: replace this with ExtensionMethodScopes-style enumerator
            for (var scope = this; scope != null; scope = scope.Next)
            {
                var newMethodGroup = MethodGroup.GetInstance();
                var diagnostics = DiagnosticBag.GetInstance();
                var lookupResult = LookupResult.GetInstance();

                scope.LookupConceptMethodsInSingleBinder(lookupResult, methodName, arity, null, options, this, false, ref useSiteDiagnostics);

                if (!lookupResult.IsClear)
                {
                    Debug.Assert(lookupResult.Symbols.Any());
                    var members = ArrayBuilder<Symbol>.GetInstance();
                    bool wasError;
                    Symbol symbol = GetSymbolOrMethodOrPropertyGroup(lookupResult, expression, methodName, arity, members, diagnostics, out wasError);
                    Debug.Assert((object)symbol == null);
                    Debug.Assert(members.Count > 0);
                    newMethodGroup.PopulateWithExtensionMethods(null, members, typeArguments, lookupResult.Kind);
                    members.Free();
                }

                lookupResult.Free();

                if (newMethodGroup.Methods.IsEmpty()) continue;
                if (analyzedArguments == null) return new MethodGroupResolution(newMethodGroup, diagnostics.ToReadOnlyAndFree());

                // @t-mawind
                //   Using extension method stuff here is a stopgap.
                //
                // We have to copy analyzed arguments, because they are freed
                // as part of freeing an extension result set.
                var args = AnalyzedArguments.GetInstance();
                args.Arguments.AddRange(analyzedArguments.Arguments);
                args.Names.AddRange(analyzedArguments.Names);
                args.RefKinds.AddRange(analyzedArguments.RefKinds);

                var overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
                bool allowRefOmittedArguments = newMethodGroup.Receiver.IsExpressionOfComImportType(); ;
                OverloadResolution.MethodInvocationOverloadResolution(newMethodGroup.Methods, newMethodGroup.TypeArguments, analyzedArguments, overloadResolutionResult, ref useSiteDiagnostics, isMethodGroupConversion, allowRefOmittedArguments);
                diagnostics.Add(expression, useSiteDiagnostics);
                var sealedDiagnostics = diagnostics.ToReadOnlyAndFree();
                var result = new MethodGroupResolution(newMethodGroup, null, overloadResolutionResult, args, methodGroup.ResultKind, sealedDiagnostics);

                // If the search in the current scope resulted in any applicable method (regardless of whether a best 
                // applicable method could be determined) then our search is complete. Otherwise, store aside the
                // first non-applicable result and continue searching for an applicable result.
                if (result.HasAnyApplicableMethod)
                {
                    if (!firstResult.IsEmpty)
                    {
                        // Free parts of the previous result but do not free AnalyzedArguments
                        // since we're using the same arguments for the returned result.
                        firstResult.MethodGroup.Free();
                        firstResult.OverloadResolutionResult.Free();
                    }
                    return result;
                }
                else if (firstResult.IsEmpty)
                {
                    firstResult = result;
                }
                else
                {
                    // Neither the first result, nor applicable. No need to save result.
                    overloadResolutionResult.Free();
                    newMethodGroup.Free();
                }
            }

            return firstResult;
        }

        bool DecideIfToUseConcepts(MethodGroupResolution methodResolution, MethodGroupResolution conceptMethodResolution)
        {
            // @t-mawind
            // This is mainly copied directly from the method/extension method
            // tie break.

            if (methodResolution.HasAnyApplicableMethod) return false;
            if (conceptMethodResolution.HasAnyApplicableMethod) return true;
            if (conceptMethodResolution.IsEmpty) return false;
            if (methodResolution.IsEmpty) return true;

            Debug.Assert(!methodResolution.HasAnyApplicableMethod);
            Debug.Assert(!conceptMethodResolution.HasAnyApplicableMethod);
            Debug.Assert(!methodResolution.IsEmpty);
            Debug.Assert(!conceptMethodResolution.IsEmpty);

            LookupResultKind methodResultKind = methodResolution.ResultKind;
            LookupResultKind extensionMethodResultKind = conceptMethodResolution.ResultKind;
            if (methodResultKind != extensionMethodResultKind &&
                methodResultKind == extensionMethodResultKind.WorseResultKind(methodResultKind))
            {
                return true;
            }

            return false;
        }
    }
}
