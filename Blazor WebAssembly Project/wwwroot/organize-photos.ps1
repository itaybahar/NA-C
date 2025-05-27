# Photo Organization Script for Homepage
# This script helps you organize your photos for the carousel

Write-Host "Photo Organization Script" -ForegroundColor Red
Write-Host "=========================" -ForegroundColor White

# Get the current directory (should be wwwroot)
$currentDir = Get-Location
$imagesDir = Join-Path $currentDir "images"

# Create images directory if it doesn't exist
if (!(Test-Path $imagesDir)) {
    New-Item -ItemType Directory -Path $imagesDir -Force
    Write-Host "Created images directory: $imagesDir" -ForegroundColor Green
}

Write-Host ""
Write-Host "This script will help you organize your photos for the carousel." -ForegroundColor Yellow
Write-Host ""

# Define the required photo names and descriptions
$photoNames = @(
    "group-photo-1.jpg",
    "group-photo-2.jpg", 
    "beach-photo.jpg",
    "sports-team-1.jpg",
    "sports-team-2.jpg",
    "stadium-crowd-1.jpg",
    "stadium-crowd-2.jpg",
    "stadium-fans.jpg"
)

$descriptions = @(
    "First group photo with military uniforms and banners",
    "Gymnasium group photo with red fabric overhead",
    "Beach sunset photo with red banner",
    "Sports team photo with red banners and uniforms",
    "Group of young women with red banners on court",
    "Stadium crowd photo with people in red",
    "Another stadium photo with fans in red and black",
    "Stadium bleacher photo with group in red and black"
)

# Option 1: Copy from another folder
Write-Host "Option 1: Copy photos from another folder" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor White
$sourceFolder = Read-Host "Enter the full path to the folder containing your photos (or press Enter to skip)"

if ($sourceFolder -and (Test-Path $sourceFolder)) {
    $sourceFiles = Get-ChildItem -Path $sourceFolder -Filter "*.jpg" | Sort-Object Name
    
    if ($sourceFiles.Count -ge 8) {
        Write-Host "Found $($sourceFiles.Count) JPG files in the source folder." -ForegroundColor Green
        Write-Host ""
        
        for ($i = 0; $i -lt 8; $i++) {
            if ($i -lt $sourceFiles.Count) {
                $sourceFile = $sourceFiles[$i].FullName
                $targetName = $photoNames[$i]
                $targetPath = Join-Path $imagesDir $targetName
                
                Write-Host "[$($i+1)/8] Copying: $($sourceFiles[$i].Name) -> $targetName" -ForegroundColor Yellow
                Write-Host "       Description: $($descriptions[$i])" -ForegroundColor Gray
                
                Copy-Item -Path $sourceFile -Destination $targetPath -Force
                Write-Host "       Success!" -ForegroundColor Green
                Write-Host ""
            }
        }
        
        Write-Host "All photos have been organized successfully!" -ForegroundColor Green
        Write-Host "Your carousel is now ready to display the photos." -ForegroundColor Green
    } else {
        Write-Host "Found only $($sourceFiles.Count) JPG files. You need at least 8 photos." -ForegroundColor Red
    }
} else {
    Write-Host "Skipping folder copy option." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Option 2: Check current status" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor White
Write-Host "Current photo status:" -ForegroundColor Yellow
Write-Host ""

for ($i = 0; $i -lt $photoNames.Count; $i++) {
    $targetPath = Join-Path $imagesDir $photoNames[$i]
    $exists = Test-Path $targetPath
    $status = if ($exists) { "EXISTS" } else { "MISSING" }
    $color = if ($exists) { "Green" } else { "Red" }
    
    Write-Host "$status - $($photoNames[$i])" -ForegroundColor $color
    Write-Host "    $($descriptions[$i])" -ForegroundColor Gray
    Write-Host ""
}

Write-Host ""
Write-Host "Target directory: $imagesDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Once all 8 photos are in place with the correct names," -ForegroundColor Green
Write-Host "your homepage carousel will automatically display them!" -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 