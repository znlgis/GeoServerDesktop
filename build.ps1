# Build script for GeoServerDesktop (Windows)

Write-Host "Building GeoServerDesktop..." -ForegroundColor Green
Write-Host ""

# Restore dependencies
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to restore packages" -ForegroundColor Red
    exit 1
}

# Build solution
Write-Host ""
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "To run the application:" -ForegroundColor Cyan
Write-Host "  dotnet run --project GeoServerDesktop.App/GeoServerDesktop.App.csproj"
Write-Host ""
Write-Host "To publish:" -ForegroundColor Cyan
Write-Host "  dotnet publish GeoServerDesktop.App/GeoServerDesktop.App.csproj -c Release -o ./publish"
