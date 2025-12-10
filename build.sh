#!/bin/bash
# Build script for GeoServerDesktop

echo "Building GeoServerDesktop..."
echo ""

# Restore dependencies
echo "Restoring NuGet packages..."
dotnet restore

if [ $? -ne 0 ]; then
    echo "Failed to restore packages"
    exit 1
fi

# Build solution
echo ""
echo "Building solution..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "Build failed"
    exit 1
fi

echo ""
echo "Build completed successfully!"
echo ""
echo "To run the application:"
echo "  dotnet run --project GeoServerDesktop.App/GeoServerDesktop.App.csproj"
echo ""
echo "To publish:"
echo "  dotnet publish GeoServerDesktop.App/GeoServerDesktop.App.csproj -c Release -o ./publish"
