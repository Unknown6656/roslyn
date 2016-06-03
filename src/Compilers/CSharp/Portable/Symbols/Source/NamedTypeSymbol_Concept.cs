// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}