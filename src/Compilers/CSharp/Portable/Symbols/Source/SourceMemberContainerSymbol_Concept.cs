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
        /// Gets whether this symbol represents an instance.
        /// </summary>
        /// <returns>
        /// True if this symbol was declared as an instance; false otherwise.
        /// </returns>
        internal override bool IsInstance => //@t-mawind
            this.MergedDeclaration.Kind == DeclarationKind.Instance;
        // This used to check HasInstanceAttribute, but this leads to infinite
        // loops.

        #endregion Concept and instance selectors
    }
}
