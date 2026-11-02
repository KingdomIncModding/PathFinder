param([string]$GameRoot = (Join-Path $PSScriptRoot '..\..'), [switch]$StageForNextLaunch)
$ErrorActionPreference = 'Stop'
$GameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
if (!(Test-Path -LiteralPath (Join-Path $GameRoot 'KingdomInc.exe'))) { throw 'Choose the KingdomInc game directory.' }
$gameRunning = [bool](Get-Process -Name KingdomInc -ErrorAction SilentlyContinue)
if ($gameRunning -and !$StageForNextLaunch) { throw 'Close Kingdom Inc before installing, or stage a new installation with -StageForNextLaunch.' }
$source = Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\PathFinder.dll'
if (!(Test-Path -LiteralPath $source)) { throw 'Build the Release project first.' }
$destination = Join-Path $GameRoot 'BepInEx\plugins\PathFinder'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$target = Join-Path $destination 'PathFinder.dll'
if (Test-Path -LiteralPath $target) {
    $backup = Join-Path $PSScriptRoot ('_Backup\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    Copy-Item -LiteralPath $target -Destination $backup
    if ($gameRunning) {
        # Keep the running game's mapped file intact, outside plugin discovery.
        # Its replacement is a new file which is only loaded on the next launch.
        $resolvedTarget = (Resolve-Path -LiteralPath $target).Path
        $resolvedBackup = (Resolve-Path -LiteralPath $backup).Path
        $expectedBackupRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '_Backup'))
        if (!$resolvedTarget.StartsWith($GameRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
            !$resolvedBackup.StartsWith($expectedBackupRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Install paths escaped their intended roots.' }
        $loaded = Join-Path $resolvedBackup 'PathFinder.loaded.dll'
        Move-Item -LiteralPath $resolvedTarget -Destination $loaded
        try { Copy-Item -LiteralPath $source -Destination $target }
        catch {
            if (!(Test-Path -LiteralPath $target)) { Move-Item -LiteralPath $loaded -Destination $target }
            throw
        }
        if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw 'Staged DLL verification failed.' }
        Write-Output "Staged Path Finder for next launch: $target"
        Write-Output "Running game retains its original mapped DLL: $loaded"
        return
    }
}
Copy-Item -LiteralPath $source -Destination $target
if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw 'Installed DLL verification failed.' }
Write-Output "Installed Path Finder: $target"
if ($gameRunning) { Write-Output 'Restart Kingdom Inc to load the new plugin.' }
