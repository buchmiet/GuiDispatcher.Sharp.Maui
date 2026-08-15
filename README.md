# GuiDispatcher.Sharp.Maui

.NET MAUI 10 implementation of
[GuiDispatcher.Sharp](https://www.nuget.org/packages/GuiDispatcher.Sharp) for
**.NET 10**.

Use this adapter in MAUI applications that need to expose the platform UI
dispatcher through the neutral `IGuiDispatcher` and `IGuiTimer` contracts.

## Install

```xml
<PackageReference Include="GuiDispatcher.Sharp.Maui" Version="1.1.*" />
```

This package pulls in:

- `GuiDispatcher.Sharp` 1.1.2 or later, below 2.0;
- `Microsoft.Maui.Core` 10.x.

## Usage

The safest option is to pass the dispatcher from an existing MAUI page, window,
or other `BindableObject`:

```csharp
using GuiDispatcher.Sharp.Contracts;
using GuiDispatcher.Sharp.Maui;

IGuiDispatcher dispatcher = new MauiGuiDispatcher(page.Dispatcher);
```

The parameterless constructor can be used when the service is created on a
MAUI UI thread:

```csharp
IGuiDispatcher dispatcher = new MauiGuiDispatcher();
```

If no dispatcher is associated with the current thread, the parameterless
constructor throws instead of silently binding the service to a worker thread.

```csharp
await dispatcher.InvokeAsync(() =>
{
    viewModel.Apply(result);
});

using var timer = dispatcher.CreateTimer(TimeSpan.FromSeconds(1));
timer.Tick += (_, _) => viewModel.Refresh();
timer.Start();
```

MAUI's native `IDispatcher.Dispatch` reports whether it accepted an operation.
This adapter turns a rejected operation into an `InvalidOperationException`
instead of returning a task that can never complete.

## Requirements

- .NET 10
- .NET MAUI 10

The adapter targets neutral `net10.0`; MAUI applications can consume it from
their Android, iOS, Mac Catalyst, Windows, and Tizen target frameworks.

## NuGet publishing

Publishing uses NuGet Trusted Publishing from GitHub Actions, without a stored
API key. Configure this policy on nuget.org before pushing the first release
tag:

| Field | Value |
|-------|-------|
| Repository owner | `buchmiet` |
| Repository | `GuiDispatcher.Sharp.Maui` |
| Workflow file | `publish-nuget.yml` |
| Environment | `production` |

### Releasing

Releases are cut by pushing a `vX.Y.Z` tag. Before tagging:

1. Bump `<Version>` in `GuiDispatcher.Sharp.Maui.csproj`.
2. Move the relevant `[Unreleased]` entries in `CHANGELOG.md` into a dated
   release section.
3. Commit both changes.
4. Tag and push: `git tag vX.Y.Z && git push origin vX.Y.Z`.

For a coordinated release of the complete package family, follow the
[family release guide](https://github.com/buchmiet/GuiDispatcher.Sharp/blob/main/RELEASING_FAMILY.md).
