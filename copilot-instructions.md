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

- **Service Layer Pattern**: Each major GeoServer resource (Workspaces, DataStores, Layers, Styles, LayerGroups) has its own service class
- **Factory Pattern**: `GeoServerClientFactory` creates service instances with shared HTTP client
- **Interface Abstraction**: `IGeoServerHttpClient` allows for dependency injection and testing
- **RESTful Design**: All API calls follow GeoServer REST conventions

### Avalonia Desktop App

- **MVVM Pattern**: Strict separation between Views (XAML), ViewModels (logic), and Models (data)
- **Dependency Injection**: Services are injected into ViewModels
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
2. Create service class in `Services/` folder
3. Add service creation method to `GeoServerClientFactory`
4. Update app ViewModels to use the new service

### Adding a New View/ViewModel

1. Create ViewModel in `ViewModels/` folder, inheriting from appropriate base
2. Create corresponding .axaml view in `Views/` folder
3. Register ViewModel with dependency injection
4. Wire up navigation/commands

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

```csharp
using GeoServerDesktop.GeoServerClient.Http;
using GeoServerDesktop.GeoServerClient.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace GeoServerDesktop.GeoServerClient.Services
{
    /// <summary>
    /// Service for managing GeoServer resources
    /// </summary>
    public class ResourceService
    {
        private readonly IGeoServerHttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the ResourceService class
        /// </summary>
        /// <param name="httpClient">HTTP client for GeoServer operations</param>
        public ResourceService(IGeoServerHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets a list of all resources
        /// </summary>
        /// <returns>Array of resources</returns>
        public async Task<Resource[]> GetResourcesAsync()
        {
            var response = await _httpClient.GetAsync("/rest/resources.json");
            var wrapper = JsonConvert.DeserializeObject<ResourceListWrapper>(response);
            return wrapper?.ResourceList?.Resources ?? new Resource[0];
        }
    }
}
```

## Testing Considerations

- Services should accept `IGeoServerHttpClient` interface for easy mocking
- Keep business logic in services, not in HTTP client
- ViewModels should accept service interfaces for testability

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
