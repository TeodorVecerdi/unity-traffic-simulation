function run {
    [CmdletBinding()]
    param(
        [ValidateSet('help', 'unity', 'rider', 'all', 'list')]
        [string]$project = 'all',

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$args
    )

    $originalLocation = Get-Location

    try {
        switch ($project) {
            "help" {
                Write-Host "Available projects:"
                Write-Host "  help - Show this help message"
                Write-Host "  unity - Open the Unity project [default]"
                Write-Host "  rider - Open the Rider project"
                Write-Host "  all - Open both Unity and Rider"
                Write-Host "  list - List all available Unity versions"
            }
            "unity" {
                _run_unity
            }
            "rider" {
                _run_rider
            }
            "all" {
                _run_unity
                _run_rider
            }
            "list" {
                Write-Host "Available Unity versions:"
                $editorPath = "C:\Program Files\Unity\Hub\Editor\"
                if (Test-Path $editorPath) {
                    Get-ChildItem $editorPath | ForEach-Object { Write-Host "  - $($_.Name)" }
                }
                else {
                    Write-Host "  Unity Hub Editor directory not found at $editorPath"
                }
            }
            default {
                Write-Error "Invalid project: $project"
                Write-Host "Use 'run help' to see available options."
            }
        }
    }
    finally {
        # Return to the original location
        Set-Location -Path $originalLocation
    }
}

function _run_unity {
    Write-Host "Running project: unity"
                
    # Use the Unity project path from environment
    $unityProjectPath = $env:MEDIA_VAULT_UNITY_PROJECT_DIR
                
    if (-not $unityProjectPath) {
        Write-Error "MEDIA_VAULT_UNITY_PROJECT_DIR environment variable not set. Make sure to run the activate script first."
        return
    }
                
    if (-not (Test-Path $unityProjectPath)) {
        Write-Error "Unity project directory not found: $unityProjectPath"
        return
    }
                
    # Read Unity version from project settings
    $versionFile = Join-Path $unityProjectPath "ProjectSettings\ProjectVersion.txt"
    if (-not (Test-Path $versionFile)) {
        Write-Error "ProjectVersion.txt not found at: $versionFile"
        return
    }
                
    $versionLine = Get-Content $versionFile | Where-Object { $_ -match "m_EditorVersion:" }
    if (-not $versionLine) {
        Write-Error "Unable to find Unity version in ProjectVersion.txt"
        return
    }
                
    $unityVersion = ($versionLine -split ": ")[1].Trim()
    Write-Host "Detected Unity version: $unityVersion"
                
    # Try to find Unity installation
    $unityPath = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
    if (-not (Test-Path $unityPath)) {
        Write-Error "Unity $unityVersion not found at $unityPath"
        Write-Host "Available versions:"
        $editorPath = "C:\Program Files\Unity\Hub\Editor\"
        if (Test-Path $editorPath) {
            Get-ChildItem $editorPath | ForEach-Object { Write-Host "  - $($_.Name)" }
        }
        else {
            Write-Host "  Unity Hub Editor directory not found at $editorPath"
        }
        return
    }
                
    Write-Host "Starting Unity..."
    Write-Host "Project: $unityProjectPath"
    Write-Host "Unity: $unityPath"
                
    # Start Unity with the project
    & "$unityPath" -projectPath "$unityProjectPath"
}

function _run_rider {
    Write-Host "Running project: rider"

    # Find `Rider.cmd` in the path
    $riderPath = Get-Command "Rider.cmd" -ErrorAction SilentlyContinue
    if (-not $riderPath) {
        Write-Error "Rider.cmd not found in the path."
        return
    }

    $unityProjectPath = Resolve-Path $env:MEDIA_VAULT_UNITY_PROJECT_DIR
    if (-not $unityProjectPath) {
        Write-Error "MEDIA_VAULT_UNITY_PROJECT_DIR environment variable not set. Make sure to run the activate script first."
        return
    }

    # Find a solution file in the root of the project
    $solutionFile = Get-ChildItem -Path $unityProjectPath -Filter "*.sln" -ErrorAction SilentlyContinue
    if (-not $solutionFile) {
        Write-Error "No solution file found in the project."
        return
    }

    $solutionPath = $solutionFile.FullName
    $relativePath = $solutionPath.Replace($unityProjectPath, "").TrimStart('\\')

    Write-Host "Starting $relativePath"
    & "Rider.cmd" "$solutionPath"
}
