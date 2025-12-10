# NuGet Package Publishing Guide

## Overview

This project uses GitHub Actions to automatically build and publish the `GeoServerDesktop.GeoServerClient` NuGet package whenever a new version tag is created.

## How to Publish a New Version

### 1. Prerequisites

Before publishing, ensure you have:
- A NuGet.org account
- An API key from NuGet.org (with push permissions)
- The API key added as a GitHub repository secret named `NUGET_API_KEY`

### 2. Setting Up the NuGet API Key

1. Go to [NuGet.org](https://www.nuget.org/) and sign in
2. Navigate to your account settings → API Keys
3. Create a new API key with "Push" permissions
4. Copy the API key
5. In your GitHub repository, go to Settings → Secrets and variables → Actions
6. Create a new repository secret:
   - Name: `NUGET_API_KEY`
   - Value: [paste your NuGet API key]

### 3. Creating a New Release

To publish a new version of the package:

1. Ensure all changes are committed and pushed to the main branch
2. Create and push a new tag with the version number:

```bash
# Format: v{major}.{minor}.{patch}
git tag v1.0.0
git push origin v1.0.0
```

3. The GitHub Action will automatically:
   - Build the project
   - Create a NuGet package with the version from the tag (e.g., `v1.0.0` → `1.0.0`)
   - Push the package to NuGet.org
   - Upload the package as a build artifact

### 4. Monitoring the Publish Process

1. Go to your repository's "Actions" tab on GitHub
2. Find the "Publish NuGet Package" workflow run
3. Check the status and logs of each step
4. Once complete, verify the package appears on [NuGet.org](https://www.nuget.org/packages/GeoServerDesktop.GeoServerClient)

## Package Information

- **Package ID**: `GeoServerDesktop.GeoServerClient`
- **Description**: A .NET Standard 2.0 library providing a complete REST API client for GeoServer
- **License**: MIT
- **Repository**: https://github.com/znlgis/GeoServerDesktop

## Version Naming Convention

Follow semantic versioning (SemVer):
- **Major version** (v1.0.0): Breaking changes
- **Minor version** (v1.1.0): New features, backwards compatible
- **Patch version** (v1.0.1): Bug fixes, backwards compatible

Examples:
- `v1.0.0` - Initial release
- `v1.1.0` - Added new features
- `v1.1.1` - Bug fixes
- `v2.0.0` - Breaking changes

## Troubleshooting

### Package Already Exists

If you see an error about the package already existing:
- The workflow includes `--skip-duplicate` flag to prevent errors
- However, you cannot replace an existing version
- Create a new tag with an incremented version number

### Build Failures

If the build fails:
1. Check the GitHub Actions logs for specific error messages
2. Ensure the project builds locally: `dotnet build src/GeoServerDesktop.GeoServerClient/GeoServerDesktop.GeoServerClient.csproj --configuration Release`
3. Fix any compilation errors and create a new tag

### Authentication Failures

If pushing to NuGet fails:
- Verify the `NUGET_API_KEY` secret is set correctly
- Ensure the API key has not expired
- Check that the API key has "Push" permissions

## Manual Publishing (Alternative)

If you need to publish manually:

```bash
# Navigate to the source directory
cd src

# Build the project
dotnet build GeoServerDesktop.GeoServerClient/GeoServerDesktop.GeoServerClient.csproj --configuration Release

# Create the package
dotnet pack GeoServerDesktop.GeoServerClient/GeoServerDesktop.GeoServerClient.csproj --configuration Release --output ./nupkg /p:PackageVersion=1.0.0

# Push to NuGet
dotnet nuget push ./nupkg/*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## Workflow File

The workflow is defined in `.github/workflows/nuget-publish.yml` and includes:
- Automatic trigger on tag creation (tags starting with `v`)
- .NET 8 SDK setup
- Dependency restoration
- Release build
- NuGet package creation
- Publishing to NuGet.org
- Artifact upload for verification
