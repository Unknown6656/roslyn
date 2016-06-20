# NMakefile for compiling the concept examples.
# Needs the experimental CSC built in the below directory.
# TODO: port to MSBuild?

CSC=..\..\Binaries\Debug\csc.exe

all: \
     101.exe \
     beautiful_differentiation.exe \
     eq_prelude.exe \
     eq_stage1.exe \
     eq_stage2.exe \
     eq_stage3.exe \
     eq_stage4.exe \
     eq_stage5.exe \
     exp_sugared.exe \
     monoid_examples.exe \
     op_beautiful_differentiation.exe \
     op_quickhull.exe \
     override.exe \
     quickhull.exe

# Each item depends on CSC to make sure compilation is re-triggered if
# CSC is updated.

monoid_examples.exe: $(CSC) monoid_examples.cs Monoid.dll Prelude.dll ConceptAttributes.dll
	$(CSC) /reference:Monoid.dll \
	       /reference:Prelude.dll \
	       /reference:ConceptAttributes.dll \
	       /out:$@ \
	       monoid_examples.cs
	peverify $@

quickhull.exe: $(CSC) quickhull.cs Monoid.dll Prelude.dll ConceptAttributes.dll
	$(CSC) /reference:Monoid.dll \
	       /reference:Prelude.dll \
	       /reference:ConceptAttributes.dll \
	       /out:$@ \
	       quickhull.cs
	peverify $@

override.exe: $(CSC) override.cs ConceptAttributes.dll
	$(CSC) /reference:ConceptAttributes.dll \
	       /out:$@ \
	       override.cs
	peverify $@

eq_stage1.exe: $(CSC) eq_stage1.cs ConceptAttributes.dll
	$(CSC) /reference:ConceptAttributes.dll \
	       /out:$@ \
	       eq_stage1.cs
	peverify $@

eq_stage2.exe: $(CSC) eq_stage2.cs ConceptAttributes.dll
	$(CSC) /reference:ConceptAttributes.dll \
	       /out:$@ \
	       eq_stage2.cs
	peverify $@

eq_stage3.exe: $(CSC) eq_stage3.cs ConceptAttributes.dll
	$(CSC) /reference:ConceptAttributes.dll \
	       /out:$@ \
	       eq_stage3.cs
	peverify $@

eq_stage4.exe: $(CSC) eq_stage4.cs ConceptAttributes.dll
	$(CSC) /reference:ConceptAttributes.dll \
	       /out:$@ \
	       eq_stage4.cs
	peverify $@

eq_stage5.exe: $(CSC) eq_stage5.cs ConceptAttributes.dll
	$(CSC) /reference:ConceptAttributes.dll \
	       /out:$@ \
	       eq_stage5.cs
	peverify $@

eq_prelude.exe: $(CSC) eq_stage5.cs Prelude.dll ConceptAttributes.dll
	$(CSC) /reference:Prelude.dll \
	       /reference:ConceptAttributes.dll \
	       /out:$@ \
	       eq_prelude.cs
	peverify $@

exp_sugared.exe: $(CSC) eq_stage5.cs ConceptAttributes.dll
	$(CSC) /reference:ConceptAttributes.dll \
	       /out:$@ \
	       exp_sugared.cs
	peverify $@

beautiful_differentiation.exe: $(CSC) beautiful_differentiation.cs Prelude.dll ConceptAttributes.dll
	$(CSC) /reference:Prelude.dll \
	       /reference:ConceptAttributes.dll \
	       /out:$@ \
	       beautiful_differentiation.cs
	peverify $@

op_beautiful_differentiation.exe: $(CSC) op_beautiful_differentiation.cs OpPrelude.dll ConceptAttributes.dll
	$(CSC) /reference:OpPrelude.dll \
	       /reference:ConceptAttributes.dll \
	       /out:$@ \
	       op_beautiful_differentiation.cs
	peverify $@

op_quickhull.exe: $(CSC) op_quickhull.cs OpMonoid.dll OpPrelude.dll ConceptAttributes.dll
	$(CSC) /reference:OpMonoid.dll \
	       /reference:OpPrelude.dll \
	       /reference:ConceptAttributes.dll \
	       /out:$@ \
	       op_quickhull.cs
	peverify $@

Monoid.dll: $(CSC) ../Monoid.cs Prelude.dll ConceptAttributes.dll
	$(CSC) /target:library \
	       /reference:Prelude.dll \
	       /reference:ConceptAttributes.dll \
	       /out:$@ \
	       ../Monoid.cs
	peverify $@

Prelude.dll: $(CSC) ../Prelude.cs ConceptAttributes.dll
	$(CSC) /target:library \
	       /reference:ConceptAttributes.dll \
	       /out:$@ \
	       ../Prelude.cs
	peverify $@

OpMonoid.dll: $(CSC) ../OpMonoid.cs OpPrelude.dll ConceptAttributes.dll
	$(CSC) /target:library \
	       /reference:OpPrelude.dll \
	       /reference:ConceptAttributes.dll \
	       /out:$@ \
	       ../OpMonoid.cs
	peverify $@

OpPrelude.dll: $(CSC) ../OpPrelude.cs ConceptAttributes.dll
	$(CSC) /target:library \
	       /reference:ConceptAttributes.dll \
	       /out:$@ \
	       ../OpPrelude.cs
	peverify $@

ConceptAttributes.dll: $(CSC) ../ConceptAttributes.cs
	$(CSC) /target:library \
	       /out:$@ \
	       ../ConceptAttributes.cs
	peverify $@
