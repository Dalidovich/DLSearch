[CmdletBinding()]
param(
    [string]$TargetDir = 'X:\Path\To\DLSearch'
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
    $signature = @'
[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);
'@
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
