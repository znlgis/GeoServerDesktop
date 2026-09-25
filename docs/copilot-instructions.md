# GitHub Copilot Instructions for GeoServerDesktop Project

## Project Overview

GeoServerDesktop is a cross-platform desktop application for managing GeoServer instances via REST API. The project consists of two main components:

1. **GeoServerDesktop.GeoServerClient** - A netstandard2.0 class library that encapsulates GeoServer REST API operations
2. **GeoServerDesktop.App** - An Avalonia-based .NET 8 desktop application providing a user-friendly interface

## Technology Stack

- **Language**: C# 12 (with netstandard2.0 compatibility for the client library)
- **Frameworks**: 
  - .NET 8 for the Avalonia desktop app
  - .NET Standard 2.0 for the GeoServer client library
- **UI Framework**: Avalonia UI (latest stable version, supporting Windows/macOS/Linux)
- **MVVM**: CommunityToolkit.Mvvm
- **JSON Serialization**: Newtonsoft.Json
- **Map Visualization**: Mapsui + Mapsui.Avalonia
- **HTTP Communication**: System.Net.Http (built-in)

## Project Structure

### GeoServerDesktop.GeoServerClient (netstandard2.0)

```
GeoServerDesktop.GeoServerClient/
├── Configuration/
│   ├── GeoServerClientOptions.cs       # Connection configuration
│   └── GeoServerClientFactory.cs       # Service factory
├── Http/
│   ├── IGeoServerHttpClient.cs         # HTTP client interface
│   ├── GeoServerHttpClient.cs          # HTTP client implementation
│   └── GeoServerRequestException.cs    # Custom exception for API errors
├── Models/
│   ├── Workspace.cs                    # Workspace models
│   ├── DataStore.cs                    # DataStore models
│   ├── Layer.cs                        # Layer models
│   ├── Style.cs                        # Style models
│   └── LayerGroup.cs                   # LayerGroup models
└── Services/
    ├── WorkspaceService.cs             # Workspace CRUD operations
    ├── DataStoreService.cs             # DataStore CRUD operations
    ├── LayerService.cs                 # Layer CRUD operations
    ├── StyleService.cs                 # Style CRUD operations
    ├── LayerGroupService.cs            # LayerGroup CRUD operations
    └── PreviewService.cs               # WMS URL generation
```

### GeoServerDesktop.App (net8.0)

```
GeoServerDesktop.App/
├── Views/                              # Avalonia XAML views
├── ViewModels/                         # View models with MVVM pattern
├── Services/                           # Application services
├── Models/                             # UI-specific models
└── Assets/                             # Images, icons, resources
```

## Architecture & Patterns

### GeoServer Client Library

- **Service Layer Pattern**: Each major GeoServer resource has its own service class
- **`ServiceBase` (M5)**: every service inherits `ServiceBase` — it owns the HTTP client (ctor null-check),
  `Esc()` path escaping, request-body builders (`JsonContent` / `JsonContentRaw` / `TextContent` /
  `BytesContent`), `GetJsonAsync<T>` / `GetWrappedAsync` response unwrapping, and `Post/PutJsonAsync` /
  `Post/PutContentAsync` senders. **Do not** hand-write `new StringContent(...)` blocks, inline
  `Uri.EscapeDataString`, or direct `_httpClient` fields in services.
- **Interface per service (M5)**: every service exposes an `IXxx` interface in `Interfaces/` (same namespace
  as the implementation). `ServiceInterfaceParityTests` fails the build-time test run if a public service
  method is missing from its interface (or an interface member drifts) — add the member to both.
- **Null handling**: request bodies must omit nulls (`JsonContent`, i.e. `NullValueHandling.Ignore`) because
  GeoServer 3.x XStream overwrites catalog objects with explicit nulls. Bodies historically sent with default
  settings are kept via `rawNulls: true` — do not "clean them up" without a measured server baseline.
- **Factory Pattern**: `GeoServerClientFactory.CreateXxxService()` still returns **concrete** types (public API
  compatibility); interfaces exist for consumers, test doubles and plugin hosts.
- **Public API compatibility**: this library ships on NuGet — new types are fine, changing/removing existing
  signatures requires a major version bump.

### Avalonia Desktop App

- **MVVM Pattern**: Strict separation between Views (XAML), ViewModels (logic), and Models (data)
- **DI composition root (M5)**: `Composition.ServiceCollectionExtensions.AddGeoServerDesktopUi()` registers
  `ISettingsService`, `IGeoServerConnectionService` and **every** ViewModel (singleton, so state survives
  navigation). `App.axaml.cs` builds the container; `MainWindowViewModel` resolves sub-VMs lazily.
  `CompositionTests` fails if any ViewModel is not registered.
- **`ViewModelBase` (M5)**: provides `IsLoading`, `StatusMessage`, `HasConnection(message)` guard and
  `RunGuardedAsync` / `RunConnectedGuardedAsync`. New view models that need GeoServer must take
  `IGeoServerConnectionService` and pass it to `base(connectionService)`; do **not** re-declare
  `IsLoading` / `StatusMessage` or hand-roll the connect guard. Prefer the guarded pipeline helpers over
  repeating `IsLoading = true; try/catch/finally`.
- **Consumers depend on abstractions (M5)**: `IGeoServerConnectionService` getters return `IXxx` interfaces,
  so VMs are substitutable (see `ServiceSubstitutionTests`).
- **Localization (M5)**: all UI strings live in `Resources/Strings.resx` (English neutral) +
  `Resources/Strings.zh.resx` (Chinese satellite); `LocalizationService` properties are
  `public string Foo => T(nameof(Foo));`. **Never** add new inline `T("en","zh")` literals — the legacy
  overload is a compatibility shim only. `LocalizationResourceTests` guards key coverage and satellite loading.
- **Tree View Navigation**: Left panel shows hierarchical resource structure
- **Detail Panel**: Right panel shows selected resource details and operations

## Coding Guidelines

### General Rules

1. **Target Framework Compatibility**:
   - GeoServerClient MUST target netstandard2.0 - avoid .NET Core/5+/6+ specific APIs
   - Do NOT use HttpClientFactory in the client library (not available in netstandard2.0)
   - Manual HttpClient lifecycle management is required

2. **Namespace Convention**:
   - Client library: `GeoServerDesktop.GeoServerClient.*`
   - Desktop app: `GeoServerDesktop.App.*`

3. **Naming Conventions** (enforced by .editorconfig):
   - Types/Methods/Properties: `PascalCase`
   - Private fields: `_camelCase` (with underscore prefix)
   - Parameters/Local variables: `camelCase`
   - Interfaces: `IPascalCase` (prefix with I)

4. **Code Style**:
   - 4 spaces for indentation
   - Opening braces on new line (Allman style)
   - `using` directives outside namespace
   - Prefer explicit types over `var` unless type is obvious

### API Interaction

1. **HTTP Requests**:
   - All REST calls go through `GeoServerHttpClient`
   - Always use JSON format (`Accept: application/json`)
   - Handle non-2xx responses with `GeoServerRequestException`

2. **JSON Serialization**:
   - Use Newtonsoft.Json for all serialization
   - GeoServer responses often have wrapper objects (e.g., `{ "workspace": { ... } }`)
   - Use `JsonProperty` attributes for proper mapping

3. **Error Handling**:
   - Catch and wrap HTTP errors in `GeoServerRequestException`
   - Include status code and response content in exceptions

### MVVM Best Practices

1. **ViewModels**:
   - Never reference UI controls directly from ViewModels
   - Use data binding and commands for all UI interactions
   - Implement `INotifyPropertyChanged` via CommunityToolkit.Mvvm

2. **Views**:
   - XAML files should be declarative with minimal code-behind
   - Use Avalonia-specific XAML syntax (.axaml extension)

3. **Separation of Concerns**:
   - Desktop app NEVER makes direct HTTP calls
   - All GeoServer interaction goes through GeoServerClient library services
   - ViewModels call services, not HTTP clients

### Map Preview with Mapsui

1. **Integration**:
   - Use `PreviewService` to generate WMS URLs
   - Mapsui consumes these URLs as tile/WMS layers
   - Map control in Avalonia view, controlled by ViewModel

2. **Workflow**:
   - LayerViewModel gets layer info
   - Uses PreviewService to generate WMS URL
   - Passes URL to MapViewModel
   - MapViewModel loads layer in Mapsui control

## Documentation Requirements

1. **XML Comments**:
   - ALL public APIs must have XML documentation comments
   - Include `<summary>`, `<param>`, `<returns>` tags as appropriate
   - Document exceptions thrown with `<exception>` tags

2. **Code Comments**:
   - Add comments only when logic is complex or non-obvious
   - Match existing comment style in the file

## Common Workflows

### Adding a New GeoServer Resource Type

1. Create model classes in `Models/` folder with JSON property mappings
2. Create service class in `Services/` folder **inheriting `ServiceBase`** (ctor `: base(httpClient)`);
   use `Esc` / `GetJsonAsync` / `Post/PutJsonAsync` / `TextContent` helpers instead of hand-rolled boilerplate
3. Add the matching `IXxxService` interface under `Interfaces/` (same namespace) and put it on the class
   declaration — `ServiceInterfaceParityTests` will fail otherwise
4. Add a `CreateXxxService()` method to `GeoServerClientFactory` (concrete return type)
5. Expose it through `IGeoServerConnectionService` (return the **interface**) and add the getter implementation
6. Add L1 offline tests (`FakeHttpClient` URL/body/parse triple assertions) and, for real endpoints,
   L2 integration tests plus a harness check; update `README` / `KNOWN-ISSUES` with any measured contract

### Adding a New View/ViewModel

1. Create ViewModel in `ViewModels/` folder inheriting `ViewModelBase`; if it needs GeoServer, take
   `IGeoServerConnectionService` in the ctor and forward it with `: base(connectionService)`, then guard
   commands with `HasConnection(...)` and run them through `RunGuardedAsync` / `RunConnectedGuardedAsync`
   (do not re-declare `IsLoading` / `StatusMessage`)
2. Add every UI string to `Resources/Strings.resx` **and** `Resources/Strings.zh.resx`, then expose
   `public string Xxx => T(nameof(Xxx));` on `LocalizationService`
3. Create the corresponding `.axaml` view + code-behind in `Views/` (ViewLocator maps by name convention)
4. Register the ViewModel in `Composition.ServiceCollectionExtensions.AddGeoServerDesktopUi()`
   (singleton) — `CompositionTests` fails if any ViewModel is unregistered
5. Wire navigation: lazily-resolved property + `Show...Command` on `MainWindowViewModel`, and a nav button
6. Add L4 headless command-flow tests (real connection + `VmRest` cross-check) — see
   `Headless/BatchOperationsViewModelTests.cs` for the current pattern

## Things to Avoid

1. **Don't** introduce incompatible libraries:
   - netstandard2.0 limitations apply to GeoServerClient
   - Check compatibility before adding NuGet packages

2. **Don't** mix concerns:
   - No UI code in GeoServerClient library
   - No direct HTTP calls in desktop app
   - No Avalonia/Mapsui dependencies in GeoServerClient

3. **Don't** break existing patterns:
   - Follow established service/model patterns
   - Maintain MVVM separation
   - Keep REST API conventions

4. **Don't** ignore errors:
   - Always handle HTTP failures
   - Provide meaningful error messages to users
   - Log important operations and errors

## AI Assistant Guidance

When generating code for this project:

1. **Check the target framework** - netstandard2.0 has different capabilities than net8.0
2. **Maintain separation** - Client library is independent of UI concerns
3. **Follow patterns** - Look at existing services/models as templates
4. **Add XML docs** - All new public APIs need documentation
5. **Consider testing** - Code should be testable with mocked dependencies
6. **Be minimal** - Don't add unnecessary complexity or features

## Example Service Implementation

M5 pattern — `ServiceBase` owns the boilerplate, the interface is part of the service contract:

```csharp
// Services/ResourceService.cs
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>Service for managing GeoServer resources.</summary>
    public class ResourceService : ServiceBase, IResourceService
    {
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public ResourceService(IGeoServerHttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <returns>Array of resources</returns>
        public async Task<Resource[]> GetResourcesAsync()
        {
            var wrapper = await GetJsonAsync<ResourceListWrapper>("/rest/resources.json");
            return wrapper?.ResourceList?.Resources ?? System.Array.Empty<Resource>();
        }

        /// POST a JSON body (nulls omitted — required by GeoServer 3.x XStream)
        public Task UploadAsync(string path, byte[] bytes) =>
            PutContentAsync("/rest/resources/" + Esc(path), BytesContent(bytes, "application/octet-stream"));
    }
}

// Interfaces/IResourceService.cs  (mirrors the public surface; parity enforced by ServiceInterfaceParityTests)
namespace GeoServerDesktop.GeoServerClient.Services
{
    public interface IResourceService
    {
        Task<Resource[]> GetResourcesAsync();
        Task UploadAsync(string path, byte[] bytes);
    }
}
```

Anti-patterns now rejected by review/tests: `private readonly IGeoServerHttpClient _httpClient;` fields,
inline `Uri.EscapeDataString`, manual `JsonConvert.SerializeObject(..., GeoServerJson.Request)` +
`new StringContent(...)` + `using` blocks in services, and service methods missing from their interface.

## Testing Considerations

- Services accept `IGeoServerHttpClient`; L1 tests use `RecordingFakeClient` (verb-ordered) or
  `PathFakeClient` (path-keyed, for aggregation services) and assert URL / request body / parsed result as a triple
- App view models are substitutable via `IGeoServerConnectionService` returning `IXxx` interfaces
  (see `ServiceSubstitutionTests`); headless L4 tests run against the real GeoServer with `VmRest` cross-checks
- Invariants enforced by tests: service↔interface parity, DI registration of every ViewModel,
  localization key coverage + Chinese satellite loading
- Data-independence rule: no dataset names, paths or expected values baked into tests — inject via
  `GSD_TEST_DATA_DIR` / `GSD_REAL_DATA_DIR` and derive expectations from the data files themselves;
  skip (with `SkipLog`) when the environment is absent

## Version Compatibility

- Target .NET 8 LTS for the desktop app
- Maintain netstandard2.0 compatibility for the client library
- Use Avalonia latest stable version
- Keep dependencies up to date but test compatibility

## Questions?

When in doubt:
- Check existing code patterns in the repository
- Refer to GeoServer REST API documentation for endpoint details
- Follow Avalonia documentation for UI components
- Consult .editorconfig for style guidelines
