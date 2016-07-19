// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Utility class for substituting actual type arguments for formal generic type parameters.
    /// </summary>
    internal sealed class MutableTypeMap : AbstractTypeParameterMap
    {
        internal MutableTypeMap()
            : base(new SmallDictionary<TypeParameterSymbol, TypeWithModifiers>())
        {
        }

        internal void Add(TypeParameterSymbol key, TypeWithModifiers value)
        {
            this.Mapping.Add(key, value);
        }

        /// <summary>
        /// Converts this type map to an immutable unification.
        /// </summary>
        /// <returns>
        /// The unification corresponding to this type map.
        /// </returns>
        internal ImmutableTypeMap ToUnification() => new ImmutableTypeMap(Mapping);
    }

    /// <summary>
    /// Immutable type map representing a unification.
    /// </summary>
    internal sealed class ImmutableTypeMap : AbstractTypeParameterMap
    {
        /// <summary>
        /// Constructs an empty unification.
        /// </summary>
        internal ImmutableTypeMap()
            : base(new SmallDictionary<TypeParameterSymbol, TypeWithModifiers>())
        {
        }

        /// <summary>
        /// Constructs a unification with the given dictionary.
        /// </summary>
        /// <param name="dict">
        /// The dictionary to use in construction.
        /// </param>
        internal ImmutableTypeMap(SmallDictionary<TypeParameterSymbol, TypeWithModifiers> dict)
            : base(new SmallDictionary<TypeParameterSymbol, TypeWithModifiers>(dict, EqualityComparer<TypeParameterSymbol>.Default))
        {
        }

        /// <summary>
        /// Merges two unifications, creating a new unification.
        /// </summary>
        /// <param name="other">
        /// The other unification to merge.</param>
        /// <returns>
        /// Null if the unifications are not disjoint (and any overlapping
        /// unifications conflict); the merged unification otherwise.
        /// </returns>
        internal ImmutableTypeMap Merge(ImmutableTypeMap other) => MergeDict(other.Mapping);

        /// <summary>
        /// Merges a unification with a dictionary, creating a new unification.
        /// </summary>
        /// <param name="dict">
        /// The dictionary representing another unification to merge.</param>
        /// <returns>
        /// Null if the unifications are not disjoint (and any overlapping
        /// unifications conflict); the merged unification otherwise.
        /// </returns>

        internal ImmutableTypeMap MergeDict(SmallDictionary<TypeParameterSymbol, TypeWithModifiers> dict)
        {
            var result = new ImmutableTypeMap(Mapping);

            foreach (var ok in dict.Keys)
            {
                // Only allow duplicate keys when they map to the same value.
                // TODO: handle error properly.
                if (Mapping.ContainsKey(ok))
                {
                    if (result.Mapping[ok] == dict[ok]) continue;
                    return null;
                }

                result.Mapping.Add(ok, dict[ok]);
            }

            return result;
        }

        /// <summary>
        /// Adds a mapping to this unification, creating a new unification.
        /// </summary>
        /// <param name="key">
        /// The key of the mapping to add.
        /// </param>
        /// <param name="value">
        /// The value of the mapping to add.
        /// </param>
        /// <returns>
        /// Null if the unifications are not disjoint (and any overlapping
        /// unifications conflict); the merged unification otherwise.
        /// </returns>

        internal ImmutableTypeMap Add(TypeParameterSymbol key, TypeWithModifiers value)
        {
            var sd = new SmallDictionary<TypeParameterSymbol, TypeWithModifiers>();
            sd.Add(key, value);
            return MergeDict(sd);
        }
    }
}
