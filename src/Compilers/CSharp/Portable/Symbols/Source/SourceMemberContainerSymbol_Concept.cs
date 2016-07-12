// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceMemberContainerTypeSymbol
    {
        #region Concept and instance selectors

        /// <summary>
        /// Gets whether this symbol represents a concept.
        /// </summary>
        /// <returns>
        /// True if this symbol is a concept (either it was declared as a
        /// concept, or it is an interface with the <c>System_Concepts_ConceptAttribute</c>
        /// attribute); false otherwise.
        /// </returns>
        internal override bool IsConcept => //@t-mawind
            this.MergedDeclaration.Kind == DeclarationKind.Concept ||
            (this.IsInterfaceType() && this.HasConceptAttribute);

        /// <summary>
        /// Gets whether this symbol represents a concept.
        /// </summary>
        /// <returns>
        /// True if this symbol is an instance (either it was declared as an
        /// instance, or it is a struct with the
        /// <c>System_Concepts_ConceptInstanceAttribute</c> attribute); false otherwise.
        /// </returns>
        internal override bool IsInstance => //@t-mawind
            this.MergedDeclaration.Kind == DeclarationKind.Instance ||
            (this.IsStructType() && this.HasInstanceAttribute);

        #endregion Concept and instance selectors

        /// <summary>
        /// Tries to construct a synthesised method to represent an access by
        /// an instance into a concept's default dictionary.
        /// </summary>
        /// <param name="conceptMethod">
        /// The method for which we are accessing the default dictionary.
        /// </param>
        /// <returns>
        /// A synthesised forwarding method that, or null if one could not be
        /// constructed.
        /// </returns>
        private SynthesizedExplicitImplementationForwardingMethod SynthesizeDefaultStructImplementation(MethodSymbol conceptMethod)
        {
            Debug.Assert(conceptMethod.ContainingType.IsConcept,
                $"Method at {nameof(SynthesizeDefaultStructImplementation)} must belong to a concept.");

            // At this stage, we don't even know if we _have_ a default
            // dictionary: we rely on the method body synthesis to bail
            // if there isn't one.

            return new SynthesizedDefaultStructImplementationMethod(conceptMethod, this);
        }
    }
}
