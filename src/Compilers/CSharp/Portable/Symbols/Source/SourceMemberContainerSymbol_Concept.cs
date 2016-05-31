// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceMemberContainerTypeSymbol
    {
        /// <summary>
        /// Gets whether this symbol has the concept attribute set.
        /// </summary>
        /// <returns>
        /// True if this symbol has the <c>ConceptAttribute</c> attribute;
        /// false otherwise.
        ///</returns>
        internal bool HasConceptAttribute //@t-mawind
        {
            get
            {
                var conceptAttr = WellKnownTypes.GetMetadataName(WellKnownType.ConceptAttribute);
                foreach (var attr in this.GetAttributes())
                {
                    // TODO: Is this the most robust way to compare types?
                    if (attr.AttributeClass.MetadataName == conceptAttr)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets whether this symbol represents a concept.
        /// </summary>
        /// <returns>
        /// True if this symbol is a concept (either it was declared as a
        /// concept, or it is an interface with the <c>ConceptAttribute</c>
        /// attribute); false otherwise.
        /// </returns>
        internal bool IsConcept => //@t-mawind
            this.MergedDeclaration.Kind == DeclarationKind.Concept ||
            (this.IsInterfaceType() && this.HasConceptAttribute);

        /// <summary>
        /// Gets whether this symbol has the instance attribute set.
        /// </summary>
        /// <returns>
        /// True if this symbol has the <c>ConceptInstanceAttribute</c>
        /// attribute; false otherwise.
        ///</returns>
        internal bool HasInstanceAttribute //@t-mawind
        {
            get
            {
                var instanceAttr = WellKnownTypes.GetMetadataName(WellKnownType.ConceptInstanceAttribute);
                foreach (var attr in this.GetAttributes())
                {
                    // TODO: Is this the most robust way to compare types?
                    if (attr.AttributeClass.MetadataName == instanceAttr)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets whether this symbol represents a concept.
        /// </summary>
        /// <returns>
        /// True if this symbol is an instance (either it was declared as an
        /// instance, or it is a struct with the
        /// <c>ConceptInstanceAttribute</c> attribute); false otherwise.
        /// </returns>
        internal bool IsInstance => //@t-mawind
            this.MergedDeclaration.Kind == DeclarationKind.Instance ||
            (this.IsStructType() && this.HasInstanceAttribute);
    }
}
