# Derive query inputs from declaration surfaces

On current `main`, source adapters can describe a reusable declaration's inputs without classifying that declaration as an event, command, query or concept. Generation can transfer those inputs to an exact query endpoint when an application adapter explicitly opts in. Plain `Reads` relationships do not copy members.

## Contribute the proof

Declare the capabilities `DeclarationInputMemberDeclaration`, `DeclarationInputMemberTypeUse` and `EntryPointInputTransfer` for the corresponding facts you emit.

- `DeclarationInputMemberDeclarationFact` gives a `DeclarationMemberKey` (exact declaration subject, optional backend occurrence and normalized Screenplay member name) and nonnegative semantic `DeclarationOrder`.
- `DeclarationInputMemberTypeUseFact` gives that same key and the complete substituted `TypeUseDefinition`. Preserve wrapper order, observed target identity and target backend occurrence. Source substitution and normalization belong to the observing frontend; Generation does not discover inherited members or resolve generic substitutions by reflection.
- `EntryPointInputTransferFact` identifies an exact method, query `ArtifactKey` and supported `Reads` role. Its subject is the method. An independent exact `EntryPointFact` must map that method occurrence to the same query.
- `MethodDeclarationExecutionFact` observes an exact method executing a reusable declaration solely for input transfer. Declare capability `MethodDeclarationExecution`. Its subject must equal `Definition.Method`; `Definition.Declaration` is the exact substituted declaration identity. `SourceScope` selects the method's backend occurrence and `TargetScope` selects the declaration's occurrence. This does not classify the declaration as an artifact, require an `ArtifactUsageDeclarationFact`, or create a `Reads` relationship.
- `MethodDeclarationExecutionDefinition.ExecutionOrder` supplies a nonnegative semantic execution position when proved. Null means unknown, never zero. Existing artifact execution producers can continue to contribute `MethodArtifactUsageFact` with `Kind = Reads` and optional `MethodArtifactUsageDefinition.ExecutionOrder`. Both paths use the same input transfer rule. Generation never uses a fact ID, file name or enumeration order as execution precedence.

Use exact evidence and declaration/method ownership. A null backend key means only the unscoped occurrence: no name matching, inherited lookup or single-candidate fallback crosses projects, overloads, substitutions or backend occurrences.

## Precedence and completeness

Direct endpoint inputs win by normalized name, including legacy properties and granular endpoint-owned members. Transferred additions follow the actual maximum direct declaration order, including sparse orders. Among transferred inputs, semantic execution order precedes source member order; the earliest proved occurrence wins a repeated name. Missing or conflicting order that could change the result is an error, not a lexical tie-break.

Both declaration and full type-use proof are required. Conflicting source orders, duplicate source orders for different names, contradictory types, unsupported shapes, ambiguous entry points and unresolved named targets produce located diagnostics. Generation omits uncertain additions for that endpoint and leaves direct inputs intact. A single execution can order its members by source member order without an execution position; multiple independent executions require proved, distinct positions. Artifact-usage and input-only observations of the same exact method, declaration and backend occurrence coalesce into one execution when their order proofs agree, retaining both observations in lineage. Different positions, or a known position combined with an unknown position for that same execution, produce a located `GEN0069` conflict rather than choosing a producer or copying inputs twice.

The built-in rule transfers only input member declaration, full type use and any exact named binding required by that type. It never transfers output semantics, event identity, defaults or inferred roles such as `Id`. Existing type-binding resolution selects named targets from the fixed base snapshot, including named primitives, concepts and typed `EventSourceId` primitives. There is no reflection or string fallback.

## Eligibility, lineage and verification

Method eligibility is selected first. Only surviving artifact usages, input-only executions, transfer intents, entry points and declaration facets participate. Failure-only executions and their lexical descendants cannot re-enter through the transfer rule.

Rule `cratis.screenplay.entry-point-input-members` version `1.0.0` operates on that fixed effective base snapshot, alongside existing derivation rules. It does not feed freshly derived members into another rule. Synthetic member IDs use the endpoint's exact identity and member name; lineage retains original intent, entry-point, execution, declaration, type, endpoint and scope evidence. Declaration-owned input facets and input-only execution observations remain provenance, never fake artifacts.

`Generate(snapshot)` retains the admitted originals and derives deterministic endpoint members for resolution, lowering, printing and Screenplay compiler verification. `Generate(contributions)` uses the same selection and transfer logic. The public released baseline is unchanged; these additive contracts and rules describe current `main`.
