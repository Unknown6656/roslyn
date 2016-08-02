// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A struct containing default implementations of concept methods.
    /// This is boiled down into a normal struct in the metadata.
    /// </summary>
    internal sealed class SynthesizedDefaultStructSymbol : SynthesizedContainer
    {
        /// <summary>
        /// The concept in which this default struct is located.
        /// </summary>
        private NamedTypeSymbol _concept;

        /// <summary>
        /// Constructs a new SynthesizedDefaultStructSymbol.
        /// </summary>
        /// <param name="name">
        /// The name of the default struct.
        /// </param>
        /// <param name="concept">
        /// The parent concept of the default struct.
        /// </param>
        public SynthesizedDefaultStructSymbol(string name, NamedTypeSymbol concept)
            : base(
                  name,
                  ImmutableArray.Create(
                      (TypeParameterSymbol) new SynthesizedWitnessParameterSymbol(
                          // @t-mawind
                          //   need to make this not clash with any typar in
                          //   the parent scopes, hence generated name.
                          GeneratedNames.MakeAnonymousTypeParameterName("witness"),
                          Location.None,
                          0,
                          null, // @t-mawind cyclic dependency!  Fixed below for now.
                          _ => ImmutableArray.Create((TypeSymbol) concept),
                          _ => TypeParameterConstraintKind.ValueType
                      )
                  ),
                  TypeMap.Empty
              )
        {

            _concept = concept;

            // @t-mawind cyclic dependency: as above: this is horrible.
            (TypeParameters[0] as SynthesizedWitnessParameterSymbol).SetOwner(this);
        }

        public override Symbol ContainingSymbol => _concept;

        // @t-mawind
        //   A default struct is, of course, a struct...
        public override TypeKind TypeKind => TypeKind.Struct;

        // @t-mawind
        //   ...and, as it has no fields, its layout is specified as the
        //   minimum allowed by the CLI spec (1).
        //   This override is necessary, as otherwise the generated PE is
        //   invalid.
        internal sealed override TypeLayout Layout =>
            new TypeLayout(LayoutKind.Sequential, 1, alignment: 0);
    }
}