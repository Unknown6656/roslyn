// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceNamedTypeSymbol
    {
        /// <summary>
        /// Gets the maximum number of possible witnesses in this symbol.
        /// </summary>
        /// <returns>
        /// The number of constraints, which is an upper bound on the number
        /// of implicit parameters.
        /// </returns>
        private int MaxWitnesses()
        {
            var maxImplicits = 0;
            // Find out the maximum number of referenced constraints.
            // There must be a better way...
            foreach (var syntaxRef in DeclaringSyntaxReferences)
            {
                var typeDecl = syntaxRef.GetSyntax() as TypeDeclarationSyntax;
                if (typeDecl != null)
                {
                    maxImplicits = Math.Max(maxImplicits, typeDecl.ConstraintClauses.Count);
                }
            }

            return maxImplicits;
        }

        /// <summary>
        /// Adds implicit witness type parameters on an declaration.
        /// </summary>
        /// <param name="diagnostics">
        /// The bag of diagnostics into which we report errors.
        /// </param>
        /// <param name="clauses">
        /// The constraints of the type declaration to examine.
        /// </param>
        /// <param name="parameterBuilder">
        /// The type parameter builder to append onto.
        /// </param>
        /// <param name="typeParameterNames">
        /// The names of existing, resolved type parameter names, plus space to
        /// install the names of the implicit parameters.
        /// </param>
        /// <param name="typeParameterVarianceKeywords">
        /// The names of existing, resolved variance keywords, plus space to
        /// install the names of the implicit parameters.
        /// </param>
        /// <param name="typeParameterMismatchReported">
        /// Whether a type parameter mismatch has been reported.
        /// </param>
        /// <param name="typeParameterCount">
        /// The number of explicit type parameters.
        /// </param>
        private void ResolveWitnessParams(DiagnosticBag diagnostics,
            SyntaxList<TypeParameterConstraintClauseSyntax> clauses,
            ref List<AbstractTypeParameterBuilder> parameterBuilder,
            ref string[] typeParameterNames,
            ref string[] typeParameterVarianceKeywords,
            ref bool typeParameterMismatchReported,
            int typeParameterCount)
        {
            var i = typeParameterCount;

            foreach (var clause in clauses)
            {
                if (!IsPossibleWitness(clause, typeParameterNames, typeParameterCount)) continue;

                var clauseName = clause.Name.Identifier.ValueText;
                var clauseLocation = clause.Name.Location;

                // This mostly shadows the existing code in SourceNamedTypeSymbol.
                // This time, we start at the end of where that code left off.

                var name = typeParameterNames[i];
                // We've just made this type parameter up.
                var location = Location.None;
                var varianceKind = typeParameterVarianceKeywords[i];

                // Is this the first reference we've seen to this explicit parameter?
                if (name == null)
                {
                    name = typeParameterNames[i] = clauseName;
                    varianceKind = typeParameterVarianceKeywords[i] = "";  // Invariant -- correct?
                    for (int j = 0; j < i; j++)
                    {
                        if (name == typeParameterNames[j])
                        {
                            typeParameterMismatchReported = true;
                            diagnostics.Add(ErrorCode.ERR_DuplicateTypeParameter, location, name);
                            goto next;
                        }
                    }

                    if (!ReferenceEquals(ContainingType, null))
                    {
                        var tpEnclosing = ContainingType.FindEnclosingTypeParameter(name);
                        if ((object)tpEnclosing != null)
                        {
                            // Type parameter '{0}' has the same name as the type parameter from outer type '{1}'
                            diagnostics.Add(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, location, name, tpEnclosing.ContainingType);
                        }
                    }
                next:;
                }
                else if (!typeParameterMismatchReported)
                {
                    // Note: the "this", below, refers to the name of the current class, which includes its type
                    // parameter names.  But the type parameter names have not been computed yet.  Therefore, we
                    // take advantage of the fact that "this" won't undergo "ToString()" until later, when the
                    // diagnostic is printed, by which time the type parameters field will have been filled in.
                    if (varianceKind != "")
                    {
                        // Dev10 reports CS1067, even if names also don't match
                        typeParameterMismatchReported = true;
                        diagnostics.Add(
                            ErrorCode.ERR_PartialWrongTypeParamsVariance,
                            declaration.NameLocations.First(),
                            this); // see comment above
                    }
                    else if (name != clauseName)
                    {
                        typeParameterMismatchReported = true;
                        diagnostics.Add(
                            ErrorCode.ERR_PartialWrongTypeParams,
                            declaration.NameLocations.First(),
                            this); // see comment above
                    }
                }
                parameterBuilder.Add(new WitnessTypeParameterBuilder(clauseName, clauseLocation, this));
                i++;
            }
        }

        /// <summary>
        /// Checks whether a clause is generating an implicit witness parameter.
        /// </summary>
        /// <param name="clause">The clause to investigate.</param>
        /// <param name="typeParameterNames">The array of known type parameter names.</param>
        /// <param name="typeParameterCount">The number of explicit type parameters.</param>
        /// <returns></returns>
        internal bool IsPossibleWitness(TypeParameterConstraintClauseSyntax clause, string[] typeParameterNames, int typeParameterCount)
        {
            for (int j = 0; j < typeParameterCount; j++)
            {
                if (typeParameterNames[j] == clause.Name.Identifier.ValueText) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Type parameter builder for instance implicit type parameters.
    /// </summary>
    internal sealed class WitnessTypeParameterBuilder : AbstractTypeParameterBuilder
    {
        private readonly string _name;
        private readonly Location _clauseLocation;
        private readonly SourceNamedTypeSymbol _owner;

        /// <summary>
        /// Creates a parameter builder for implicit type parameters.
        /// </summary>
        /// <param name="name">
        /// The name of the parameter.
        /// </param>
        /// <param name="clauseLocation">
        /// The location of the clause creating this parameter.
        /// </param>
        /// <param name="owner">
        /// The parent of the type parameter.
        /// </param>
        internal WitnessTypeParameterBuilder(string name, Location clauseLocation, SourceNamedTypeSymbol owner)
        {
            _name = name;
            _clauseLocation = clauseLocation;
            _owner = owner;
        }

        // @t-mawind TODO: move somewhere else
        internal override TypeParameterSymbol MakeSymbol(int ordinal, IList<AbstractTypeParameterBuilder> builders, DiagnosticBag diagnostics)
        {
            return new SynthesizedWitnessParameterSymbol(_name, _clauseLocation, ordinal, _owner);
        }
    }
}
