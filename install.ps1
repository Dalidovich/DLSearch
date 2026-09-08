[CmdletBinding()]
param(
    [string]$TargetDir = 'X:\Path\To\DLSearch'
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
    $signature = @'
[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);
'@
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
