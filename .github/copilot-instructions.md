# Copilot Instructions

## Project Guidelines
- Prefer to implement new projects on .NET 10.0 where compatibility allows; use lower target frameworks only when required by tooling such as Visual Studio Extensibility SDK.
- This extension should be implemented using VisualStudio.Extensibility.
- Assemblies used for XAML tag discovery will always be located in the `@assemblies` directory at the project root.
- Use the existing metadata-layer assembly resolver instead of introducing parallel project-resolution logic in the language server when implementing metadata loading behavior.
- For metadata preloading in the language server, do not add separate warmup abstractions; use existing classes and switch lazy metadata loading to startup-time loading.