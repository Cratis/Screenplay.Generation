<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Compatibility and support policy

This policy applies to the four Screenplay Generation packages:

- `Cratis.Screenplay.Generation.Contracts`
- `Cratis.Screenplay.Generation`
- `Cratis.Screenplay.Generation.DotNet`
- `Cratis.Screenplay.Generation.DotNet.Vogen`

## Versioning contract

The four packages are released in version lockstep. Use the same version for every directly referenced Screenplay Generation package.

Screenplay Generation is in SemVer's `0.x` initial-development phase and remains a preview. Beginning with `0.7.0`, this project nevertheless applies the following compatibility policy to its public package surface:

- `0.7.0` is the current public compatibility floor.
- Patch releases contain source- and binary-compatible fixes.
- Minor releases add public capabilities without removing or incompatibly changing existing public signatures.
- Public API removals and incompatible signature changes occur only in a declared major release.
- A public API planned for removal is marked `[Obsolete]` with migration guidance for at least one full minor release before removal in a major release.

The latest released minor line is serviced. The `0.1.0` core package surface and `0.5.0` Vogen package surface are compatibility ancestry, not serviced release lines. Consumers should upgrade all four packages together to the latest minor.

## Compatibility verification

After every release, `PackageValidationBaselineVersion` advances to that release for all four packages. Package validation allows compatible additions but rejects public removals and signature breaks. The current baseline is the latest released version, `0.8.0`; `0.7.0` remains the minimum public compatibility floor.

Public fact discriminator enums reserve `Unknown = -1` and never renumber an existing value. Unknown or future undefined values fail closed with typed diagnostics rather than falling through to another role. `GenerationDiagnostic.Outcome` is an additive, nullable semantic dimension (`Unknown`, `Conflict`, or `Unsupported`) independent of stable diagnostic code and severity.

Legacy binary smoke tests remain compiled against the `0.1.0` Contracts, Generation, and DotNet packages and the `0.5.0` DotNet.Vogen package. Those binaries continue to run unchanged against current packages until an intentional major-version compatibility reset. A separate current-source consumer compiles only against candidate packages and exercises the current public composition surface.

Published diagnostic codes are stable contracts. A diagnostic code may be retired, but it is never reused for a different condition.

Packages are ordinarily left listed so existing restores remain reproducible. The exception is a version proven to have been published from incorrectly sourced artifacts. `Cratis.Screenplay.Generation.DotNet.Vogen` versions `0.2.0`, `0.3.0`, and `0.4.0` are the identified historical exception and remain tracked for manual unlisting in [issue #13](https://github.com/Cratis/Screenplay.Generation/issues/13).

## Ownership and independent versions

Hosts own workspace and project loading, establishment of the authoritative authored-syntax-tree set, explicit source identity/display policy, generator package discovery and admission, orchestration, and output destinations. Adapters own source-framework semantics and contribute neutral facts, evidence, provenance, and diagnostics. Screenplay Generation owns deterministic resolution, lowering to the Screenplay AST, canonical text, and compiler verification.

`SourceRange.Path` is the host-declared display path. A host can add `SourceRange.FileIdentity` through the immutable factory-created `DotNetProjectSourceContext`; canonical ordering uses that stable project-qualified identity only when present. Legacy and identity-bearing evidence and diagnostics share one length-delimited structural canonical encoding with an explicit identity-presence component, so separator-bearing values cannot alias fields or cross the identity boundary. `Ordinal` preserves NFC-normalized identity casing, while `InvariantLowercase` folds with invariant lowercase and then NFC-normalizes the folded identity. Roslyn `SyntaxTree.FilePath` can retain an absolute physical checkout path, but stable identity and display values never do. Different display-root policies are valid compatibility dimensions and must be reported by the host rather than compared as unexplained byte drift. Legacy `SourceRoot` hosts retain the exact pre-context path behavior.

The following versions are intentionally independent and must not be inferred from one another:

- external adapter protocol or schema versions;
- adapter implementation versions;
- Screenplay language/compiler versions;
- Cratis Screenplay Generation package versions;
- Cratis CLI versions.

A host must evaluate each compatibility dimension explicitly rather than treating package-version equality as semantic or protocol compatibility.
