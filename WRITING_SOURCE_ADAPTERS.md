<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Writing a .NET source adapter

The canonical source-adapter onboarding guide now lives at [`Documentation/guides/build-source-adapter.md`](Documentation/guides/build-source-adapter.md).

This compatibility page remains so existing links continue to work. The guide covers:

- package selection and version availability;
- the `IDotNetScreenplayAdapter` contract;
- host-owned workspace loading, authored-tree authority, and stable source paths;
- neutral facts, evidence strength, placement, and fail-closed diagnostics;
- adapter composition, deterministic specifications, package-consumer verification, and release commands.

A source adapter interprets one framework's authored source and contributes neutral facts. It does not load workspaces, run applications, construct Screenplay syntax, or print `.play` text. Use the [canonical source adapter guide](Documentation/guides/build-source-adapter.md) for the complete contract and current examples.
