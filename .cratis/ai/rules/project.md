# Screenplay.Generation — project context

Framework-neutral source adapter SDK for generating verified Cratis Screenplay
definitions. Part of the model-first layer; owns source-to-Screenplay
recovery: adapters contribute typed semantics, the SDK lowers them through
the shared generation SDK, prints canonical `.play` source, and verifies it
with the Screenplay compiler. C#/.NET.

## Project concerns

Read every concern below before working in this repository. Together they are the project-owned instructions and override conflicting shared guidance.

- [Commands](.cratis/ai/rules/project/commands.md)
- [AI-assisted development](.cratis/ai/rules/project/ai-assisted-development.md)
