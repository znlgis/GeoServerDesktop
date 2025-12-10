# GeoServerDesktop

A cross-platform desktop application for managing GeoServer instances through a user-friendly interface.

## Overview

GeoServerDesktop provides a modern, cross-platform desktop tool for interacting with GeoServer via its REST API. Built with Avalonia UI and .NET 8, it runs on Windows, macOS, and Linux.

### Features

- **Workspace Management**: Create, view, update, and delete GeoServer workspaces
- **Data Store Management**: Manage PostGIS, Shapefile, and other data stores
- **Layer Management**: Publish, configure, and manage layers and feature types
- **Style Management**: Upload, edit, and manage SLD styles
- **Layer Groups**: Create and manage layer groups
- **Map Preview**: Built-in map preview using Mapsui for WMS layers
- **Connection Management**: Save and manage multiple GeoServer connections

## Architecture

The solution consists of two main projects:

### 1. GeoServerDesktop.GeoServerClient

A .NET Standard 2.0 class library that provides a complete REST API client for GeoServer:

- **HTTP Layer**: Handles authentication, requests, and error handling
- **Models**: Strongly-typed models for all GeoServer resources
- **Services**: High-level service APIs for each resource type
- **Preview Service**: Generates WMS URLs for map visualization

### 2. GeoServerDesktop.App

An Avalonia-based .NET 8 desktop application:

- **MVVM Architecture**: Clean separation of concerns
- **Tree View Navigation**: Hierarchical resource browser
- **Detail Views**: Rich editors for each resource type
- **Map Integration**: Mapsui-powered map preview
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Technology Stack

- **Language**: C# 12
- **Frameworks**: .NET 8, .NET Standard 2.0
- **UI**: Avalonia UI
- **MVVM**: CommunityToolkit.Mvvm
- **Maps**: Mapsui + Mapsui.Avalonia
- **JSON**: Newtonsoft.Json

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- A running GeoServer instance (for testing)

### Building the Solution

```bash
# Clone the repository
git clone https://github.com/znlgis/GeoServerDesktop.git
cd GeoServerDesktop

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run --project GeoServerDesktop.App/GeoServerDesktop.App.csproj
```

### Development

The solution can be opened in:
- Visual Studio 2022 or later
- JetBrains Rider
- Visual Studio Code (with C# extension)

## Project Structure

```
GeoServerDesktop/
├── GeoServerDesktop.sln                    # Solution file
├── .editorconfig                           # Code style configuration
├── copilot-instructions.md                 # AI assistant guidelines
├── README.md                               # This file
├── GeoServerDesktop.GeoServerClient/       # REST API client library
│   ├── Configuration/                      # Client configuration
│   ├── Http/                              # HTTP client infrastructure
│   ├── Models/                            # Data models
│   └── Services/                          # Service layer
└── GeoServerDesktop.App/                   # Avalonia desktop application
    ├── Views/                             # XAML views
    ├── ViewModels/                        # View models
    ├── Services/                          # Application services
    ├── Models/                            # UI models
    └── Assets/                            # Resources
```

## Usage

### Connecting to GeoServer

1. Launch the application
2. Navigate to Connection settings
3. Enter your GeoServer details:
   - Base URL (e.g., `http://localhost:8080/geoserver`)
   - Username (default: `admin`)
   - Password (default: `geoserver`)
4. Click Connect

### Managing Resources

- **Workspaces**: Browse and manage workspaces in the tree view
- **Data Stores**: View and configure data sources
- **Layers**: Publish new layers, edit existing ones
- **Styles**: Upload SLD files or edit styles
- **Preview**: Click any layer to preview it on the map

## API Usage

The GeoServerClient library can be used independently in your own projects:

```csharp
using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Services;

// Configure connection
var options = new GeoServerClientOptions
{
    BaseUrl = "http://localhost:8080/geoserver",
    Username = "admin",
    Password = "geoserver"
};

// Create factory
var factory = new GeoServerClientFactory(options);

// Use services
var workspaceService = factory.CreateWorkspaceService();
var workspaces = await workspaceService.GetWorkspacesAsync();

foreach (var workspace in workspaces)
{
    Console.WriteLine($"Workspace: {workspace.Name}");
}

// Clean up
factory.Dispose();
```

## Configuration

### Application Settings

Settings are stored in a local JSON configuration file:
- Windows: `%APPDATA%\GeoServerDesktop\settings.json`
- macOS: `~/Library/Application Support/GeoServerDesktop/settings.json`
- Linux: `~/.config/GeoServerDesktop/settings.json`

### Code Style

The project uses `.editorconfig` to enforce consistent coding standards:
- 4 spaces for indentation
- Allman brace style
- Private fields with `_` prefix
- XML documentation for public APIs

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Follow the code style defined in `.editorconfig`
2. Add XML documentation for public APIs
3. Follow the MVVM pattern in the desktop app
4. Keep the client library independent of UI concerns
5. Write meaningful commit messages

## GeoServer Compatibility

This client is designed to work with GeoServer 2.x REST API. It has been tested with:
- GeoServer 2.20+
- GeoServer 2.21+
- GeoServer 2.22+

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap

Planned features:
- [ ] Advanced layer configuration
- [ ] Coverage store support
- [ ] WFS service configuration
- [ ] Security and authentication management
- [ ] Bulk operations
- [ ] Import/export functionality
- [ ] Settings synchronization
- [ ] Plugin system

## Support

For issues, questions, or contributions:
- **Issues**: [GitHub Issues](https://github.com/znlgis/GeoServerDesktop/issues)
- **Discussions**: [GitHub Discussions](https://github.com/znlgis/GeoServerDesktop/discussions)

## Acknowledgments

- [GeoServer](http://geoserver.org/) - Open source server for sharing geospatial data
- [Avalonia UI](https://avaloniaui.net/) - Cross-platform XAML framework
- [Mapsui](https://mapsui.com/) - Map component for .NET applications

## Resources

- [GeoServer REST API Documentation](https://docs.geoserver.org/latest/en/user/rest/)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [Mapsui Documentation](https://mapsui.com/documentation/)
