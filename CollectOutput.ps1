param (
    [string]$buildConfig = "Debug"
)

# Get the current script's directory (assumes the script is in the root of the solution)
$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

# Define the source and destination directories relative to the script's location
$solutionDirectory = "$scriptDirectory"  # Assuming the script is in the solution root
$outputDirectory = "$scriptDirectory\OutputDlls"  # Target directory relative to the solution

# Clean the output directory
if (Test-Path $outputDirectory) {
    Remove-Item -Recurse -Force $outputDirectory
}
New-Item -ItemType Directory -Path $outputDirectory


# Search for project bin folders and copy DLLs
$projectDirectories = Get-ChildItem -Path $solutionDirectory -Recurse -Directory | Where-Object {
    Test-Path "$($_.FullName)\bin\$buildConfig\net8.0"
}

foreach ($projectDir in $projectDirectories) {
    $sourceDir = "$($projectDir.FullName)\bin\$buildConfig\net8.0"
    Write-Host "Checking: $sourceDir"

    if (Test-Path $sourceDir) {
        $dllFiles = Get-ChildItem -Path $sourceDir -Filter "*.dll"
        foreach ($dll in $dllFiles) {
            Copy-Item -Path $dll.FullName -Destination $outputDirectory
        }
    } else {
        Write-Host "Directory does not exist: $sourceDir"
    }
}


# Remove unwanted DLLs from the output directory
$excludedFiles = @("YawGLAPI.dll") # Add other files to exclude as needed
foreach ($excludedFile in $excludedFiles) {
    $filePath = Join-Path -Path $outputDirectory -ChildPath $excludedFile
    if (Test-Path $filePath) {
        Remove-Item -Path $filePath -Force
        Write-Host "Deleted: $excludedFile from $outputDirectory"
    } else {
        Write-Host "File not found for deletion: $excludedFile"
    }
}

Write-Host "All DLLs have been copied to $outputDirectory"
