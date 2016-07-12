// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedDefaultStructImplementationMethod : SynthesizedExplicitImplementationForwardingMethod
    {
        // @t-mawind
        //   The entire existence of this class is a horrific hack.

        public SynthesizedDefaultStructImplementationMethod(MethodSymbol conceptMethod, NamedTypeSymbol implementingType)
            : base(conceptMethod, conceptMethod, implementingType)
        {
        }

        public override string Name => ImplementingMethod.Name;
        public override string MetadataName => ImplementingMethod.MetadataName;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var concept = ImplementingMethod.ContainingType;
            Debug.Assert(concept.IsConcept, "Tried to synthesise default struct implementation on a non-concept interface");
            
            var instance = ContainingType;
            Debug.Assert(instance.IsInstance, "Tried to synthesise default struct implementation for a non-instance");

            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentMethod = OriginalDefinition;

            try
            {

                // Now try to find the default struct using the instance's scope...
                var binder = new BinderFactory(compilationState.Compilation, instance.GetNonNullSyntaxNode().SyntaxTree).GetBinder(instance.GetNonNullSyntaxNode());

                var ignore = new HashSet<DiagnosticInfo>();
                var defs = concept.GetDefaultStruct(binder, false, ref ignore);

                if (defs == null)
                {
                    // TODO: error
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                // Suppose the target concept is Foo<A, B>.
                // Then, the default must take type parameters <A, B, FooAB>,
                // where FooAB : Foo<A, B>.
                if (defs.Arity != concept.Arity + 1)
                {
                    // TODO: error
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                // Due to above, arity must be at least 1.

                var witnessPar = defs.TypeParameters[defs.Arity - 1];

                if (!witnessPar.IsConceptWitness)
                {
                    // TODO: error
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                var newTypeArgumentsB = ArrayBuilder<TypeWithModifiers>.GetInstance();
                foreach (var ta in concept.TypeArguments)
                {
                    // TODO: this is wrong, what if the types have modifiers?
                    newTypeArgumentsB.Add(new TypeWithModifiers(ta));
                }
                newTypeArgumentsB.Add(new TypeWithModifiers(instance));
                var newTypeArguments = newTypeArgumentsB.ToImmutableAndFree();

                // Now make the receiver for the call.  As usual, it's a default().
                var recvType = new ConstructedNamedTypeSymbol(defs, newTypeArguments);
                var receiver = F.Default(recvType);

                // Transfer over the formal parameters of this method into the arguments of the call.
                var argumentsB = ArrayBuilder<BoundExpression>.GetInstance();
                foreach (var p in ImplementingMethod.Parameters)
                {
                    argumentsB.Add(F.Parameter(p));
                }
                var arguments = argumentsB.ToImmutableAndFree();

                var call = F.MakeInvocationExpression(BinderFlags.None, F.Syntax, receiver, ImplementingMethod.Name, arguments, diagnostics, ImplementingMethod.TypeArguments);
                var block = F.Block(F.Return(call));

                F.CloseMethod(block);
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }
}
