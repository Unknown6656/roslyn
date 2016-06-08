// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that consults all of the witnesses of a method or class.
    /// </summary>
    internal class WithWitnessesBinder : Binder
    {
        /// <summary>
        /// The set of witnesses to use in binding.
        /// </summary>
        private readonly ImmutableArray<TypeParameterSymbol> _witnesses;

        /// <summary>
        /// Constructs a witness binder.
        /// </summary>
        /// <param name="parent">
        /// The method or class containing the witnesses.
        /// </param>
        /// <param name="next">
        /// The next binder in the binding chain.
        /// </param>
        internal WithWitnessesBinder(Symbol parent, Binder next)
            : base(next)
        {
            // This binder should only be used on methods or named types,
            // since these are the only places with witnesses.
            Debug.Assert(
                parent.Kind == SymbolKind.NamedType
                || parent.Kind == SymbolKind.Method
            );

            // The witnesses are inside the type parameters: we need to
            // filter them.
            var tps = ImmutableArray<TypeParameterSymbol>.Empty;
            if (parent.Kind == SymbolKind.NamedType)
            {
                tps = ((NamedTypeSymbol)parent).TypeParameters;
            }
            else if (parent.Kind == SymbolKind.Method)
            {
                tps = ((MethodSymbol)parent).TypeParameters;
            }

            var witnesses = new ArrayBuilder<TypeParameterSymbol>();
            foreach (var tp in tps)
            {
                if (tp.IsConceptWitness) witnesses.Add(tp);
            }

            _witnesses = witnesses.ToImmutableAndFree();
        }

        internal override void LookupSymbolsInSingleBinder(
        LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            foreach (var witness in _witnesses)
            {
                LookupSymbolsInWitness(witness, result, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
            }
        }
    }
}
