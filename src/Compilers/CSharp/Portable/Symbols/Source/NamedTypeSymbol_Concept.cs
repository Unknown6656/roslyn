// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class NamedTypeSymbol
    {
        /// <summary>
        /// Gets whether this symbol has the concept attribute set.
        /// </summary>
        /// <returns>
        /// True if this symbol has the <c>System_Concepts_ConceptAttribute</c> attribute;
        /// false otherwise.
        ///</returns>
        internal bool HasConceptAttribute
        { //@t-mawind
            get
            {
                foreach (var attribute in this.GetAttributes())
                {
                    if (attribute.IsTargetAttribute(this, AttributeDescription.ConceptAttribute))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets whether this symbol has the instance attribute set.
        /// </summary>
        /// <returns>
        /// True if this symbol has the <c>System_Concepts_ConceptInstanceAttribute</c>
        /// attribute; false otherwise.
        ///</returns>
        internal bool HasInstanceAttribute //@t-mawind
        {
            get
            {
                foreach (var attribute in this.GetAttributes())
                {
                    if (attribute.IsTargetAttribute(this, AttributeDescription.ConceptInstanceAttribute))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        //@t-mawind move?
        private int _implicitTypeParameterCount = -1;

        /// <summary>
        /// Returns the number of implicit type parameters.
        /// </summary>
        internal virtual int ImplicitTypeParameterCount
        {
            get
            {
                if (-1 == _implicitTypeParameterCount)
                {
                    var count = ConceptWitnesses.Length;
                    Interlocked.CompareExchange(ref _implicitTypeParameterCount, count, -1);
                }
                return _implicitTypeParameterCount;
            }
        }

        /// <summary>
        /// Gets whether this type is a default struct.
        /// </summary>
        internal virtual bool IsDefaultStruct //@t-mawind
        {
            get
            {
                foreach (var attribute in this.GetAttributes())
                {
                    if (attribute.IsTargetAttribute(this, AttributeDescription.ConceptDefaultAttribute))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the name of this type's associated default struct.
        /// </summary>
        internal string DefaultStructName
        {
            get
            {
                Debug.Assert(IsConcept, "Should never get the default struct name of a non-concept");
                // @t-mawind TODO: use a non-referenceable name
                return $"{Name}_default";
            }
        }

        /// <summary>
        /// Attempts to find this concept's associated default struct in a binder.
        /// </summary>
        /// <param name="binder">
        /// The binder in which we are looking up the default struct.
        /// </param>
        /// <param name="diagnose">
        /// Whether the lookup should emit diagnostics into
        /// <paramref name="useSiteDiagnostics"/>.
        /// </param>
        /// <param name="useSiteDiagnostics">
        /// The set of use-site diagnostics to populate with any found during
        /// lookup.
        /// </param>
        /// <returns>
        /// Null, if the default struct was not found; the struct, otherwise.
        /// </returns>
        internal NamedTypeSymbol GetDefaultStruct(Binder binder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(IsConcept, "Should never get the default struct of a non-concept");

            foreach (var m in GetTypeMembers())
            {
                if (m.IsDefaultStruct) return m;
            }

            return null;
        }

        /// <summary>
        /// Returns the type parameters of this type that are concept
        /// witnesses.
        /// </summary>
        internal ImmutableArray<TypeParameterSymbol> ConceptWitnesses
        {
            // @t-mawind TODO: this is possibly very slow, cache it?
            get
            {
                var builder = new ArrayBuilder<TypeParameterSymbol>();
                var allParams = TypeParameters;
                int numParams = allParams.Length;
                for (int i = 0; i < numParams; i++)
                {
                    if (allParams[i].IsConceptWitness) builder.Add(allParams[i]);
                }
                return builder.ToImmutableAndFree();
            }
        }
    }
}