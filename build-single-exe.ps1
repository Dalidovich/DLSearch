[CmdletBinding()]
param(
    [string]$ProjectDir = $PSScriptRoot,
    [string]$OutputDir = 'X:\Path\To\DLSearch',
    [string]$Runtime = 'win-x64',
    [string]$Configuration = 'Release',
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$msg) {
    Write-Host "`n=== $msg ===" -ForegroundColor Cyan
}

function Assert-SafeOutputDir([string]$path) {
    if (-not $path) { throw 'OutputDir is required, for example: -OutputDir "X:\Path\To\DLSearch"' }
    $full = [System.IO.Path]::GetFullPath($path)
    $protected = @(
        [System.IO.Path]::GetPathRoot($full)
        $env:USERPROFILE
        $env:SystemRoot
        $env:ProgramFiles
        ${env:ProgramFiles(x86)}
        $PSScriptRoot
        $ProjectDir
    ) | Where-Object { $_ }

    foreach ($candidate in $protected) {
        if ($full.TrimEnd('\') -ieq [System.IO.Path]::GetFullPath($candidate).TrimEnd('\')) {
            throw "Refusing to use -OutputDir '$full': the script wipes that directory before copying. Pick a dedicated folder."
        }
    }
    return $full
}

function Write-ScriptFile([string]$path, [string]$body) {
    $normalized = ($body -replace "`r`n", "`n") -replace "`n", "`r`n"
    [System.IO.File]::WriteAllText($path, $normalized, (New-Object System.Text.UTF8Encoding($true)))
}

$InstallScriptBody = @'
[CmdletBinding()]
param(
    [string]$TargetDir = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'

function Get-UserPathEntry {
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Environment', $true)
    if ($null -eq $key) {
        $key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Environment')
    }

    $raw = $key.GetValue('Path', $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
    if ($null -eq $raw) {
        $raw = ''
        $kind = [Microsoft.Win32.RegistryValueKind]::ExpandString
    }
    else {
        $kind = $key.GetValueKind('Path')
    }

    return [pscustomobject]@{
        Key   = $key
        Value = [string]$raw
        Kind  = $kind
    }
}

function Test-SamePath([string]$left, [string]$right) {
    if (-not $left -or -not $right) { return $false }
    return $left.Trim().TrimEnd('\') -ieq $right.Trim().TrimEnd('\')
}

function Publish-EnvironmentChange {
    $signature = '[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);'
    $native = Add-Type -MemberDefinition $signature -Name 'EnvironmentBroadcast' -Namespace 'DLSearch' -PassThru
    $result = [UIntPtr]::Zero
    $native::SendMessageTimeout([IntPtr]0xffff, 0x1A, [UIntPtr]::Zero, 'Environment', 2, 5000, [ref]$result) | Out-Null
}

$targetFull = [System.IO.Path]::GetFullPath($TargetDir).TrimEnd('\')

if (-not (Test-Path $targetFull)) {
    Write-Warning "Directory does not exist yet: $targetFull. Run build-single-exe.ps1 to create it."
}

$entry = Get-UserPathEntry
try {
    $parts = @($entry.Value -split ';')

    foreach ($part in $parts) {
        if (Test-SamePath $part $targetFull) {
            Write-Host "Already present in user PATH: $targetFull" -ForegroundColor Yellow
            return
        }
    }

    if ($entry.Value.Length -eq 0) {
        $updated = $targetFull
    }
    elseif ($entry.Value.EndsWith(';')) {
        $updated = $entry.Value + $targetFull + ';'
    }
    else {
        $updated = $entry.Value + ';' + $targetFull
    }
    $entry.Key.SetValue('Path', $updated, $entry.Kind)
    Publish-EnvironmentChange

    Write-Host "Added to user PATH: $targetFull" -ForegroundColor Green
    Write-Host "Type 'f <pattern>' in the Explorer address bar to search the open folder." -ForegroundColor Green
}
catch {
    Write-Host "Failed to update user PATH: $($_.Exception.Message)" -ForegroundColor Red
    throw
}
finally {
    $entry.Key.Dispose()
}
'@

$UninstallScriptBody = @'
[CmdletBinding()]
param(
    [string]$TargetDir = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'

function Get-UserPathEntry {
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Environment', $true)
    if ($null -eq $key) { return $null }

    $raw = $key.GetValue('Path', $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
    if ($null -eq $raw) {
        $key.Dispose()
        return $null
    }

    return [pscustomobject]@{
        Key   = $key
        Value = [string]$raw
        Kind  = $key.GetValueKind('Path')
    }
}

function Test-SamePath([string]$left, [string]$right) {
    if (-not $left -or -not $right) { return $false }
    return $left.Trim().TrimEnd('\') -ieq $right.Trim().TrimEnd('\')
}

function Publish-EnvironmentChange {
    $signature = '[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);'
    $native = Add-Type -MemberDefinition $signature -Name 'EnvironmentBroadcast' -Namespace 'DLSearch' -PassThru
    $result = [UIntPtr]::Zero
    $native::SendMessageTimeout([IntPtr]0xffff, 0x1A, [UIntPtr]::Zero, 'Environment', 2, 5000, [ref]$result) | Out-Null
}

$targetFull = [System.IO.Path]::GetFullPath($TargetDir).TrimEnd('\')

$entry = Get-UserPathEntry
if ($null -eq $entry) {
    Write-Host "User PATH is not set, nothing to remove: $targetFull" -ForegroundColor Yellow
    return
}

try {
    $parts = @($entry.Value -split ';')
    $kept = @($parts | Where-Object { -not (Test-SamePath $_ $targetFull) })

    if ($kept.Count -eq $parts.Count) {
        Write-Host "Not present in user PATH, nothing to remove: $targetFull" -ForegroundColor Yellow
        return
    }

    $updated = $kept -join ';'
    $entry.Key.SetValue('Path', $updated, $entry.Kind)
    Publish-EnvironmentChange

    Write-Host "Removed from user PATH: $targetFull" -ForegroundColor Green
}
catch {
    Write-Host "Failed to update user PATH: $($_.Exception.Message)" -ForegroundColor Red
    throw
}
finally {
    $entry.Key.Dispose()
}
'@

$CsprojPath = Join-Path $ProjectDir 'DLSearch.csproj'
$PublishDir = Join-Path $ProjectDir "bin\$Configuration\net9.0\$Runtime\publish"

if (-not (Test-Path $CsprojPath)) {
    throw "Project not found: $CsprojPath"
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet SDK not found in PATH'
}

$OutputDir = Assert-SafeOutputDir $OutputDir

Write-Step 'Publishing single-file exe'
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
& dotnet publish $CsprojPath `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=false `
    -p:DebugType=None -p:DebugSymbols=false `
    -p:NoWarn=MSB3246
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

Write-Step "Copying to $OutputDir"
Get-Process -Name 'f' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -like "$OutputDir*" } |
    Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir | Out-Null
Copy-Item (Join-Path $PublishDir '*') $OutputDir -Recurse -Force

$exePath = Join-Path $OutputDir 'f.exe'
if (-not (Test-Path $exePath)) { throw "Expected executable not found: $exePath" }
$sizeMb = [math]::Round((Get-Item $exePath).Length / 1MB, 1)

Write-Step 'Writing install scripts'
$installPath = Join-Path $OutputDir 'install.ps1'
$uninstallPath = Join-Path $OutputDir 'uninstall.ps1'
Write-ScriptFile $installPath $InstallScriptBody
Write-ScriptFile $uninstallPath $UninstallScriptBody
Write-Host "Created: $installPath"
Write-Host "Created: $uninstallPath"

Write-Host "`nDone. Artifacts in: $OutputDir" -ForegroundColor Green
Write-Host "Executable   : $exePath ($sizeMb MB)" -ForegroundColor Green
Write-Host "Next step    : & `"$installPath`"" -ForegroundColor Green
Write-Host "To remove    : & `"$uninstallPath`"" -ForegroundColor Green

if ($Launch) {
    Start-Process -FilePath $exePath -WorkingDirectory $OutputDir
}
