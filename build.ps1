param(
    [string]$GameRoot = ''
)

$ErrorActionPreference = 'Stop'

function Resolve-ValidatedGameRoot([string]$explicitRoot) {
    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($explicitRoot)) {
        $candidates += $explicitRoot
    } else {
        $candidates += (Join-Path $PSScriptRoot '..\..')
    }
    foreach ($candidate in $candidates) {
        if (-not (Test-Path -LiteralPath $candidate)) { continue }
        $full = (Resolve-Path -LiteralPath $candidate).Path.TrimEnd('\', '/')
        if ((Test-Path -LiteralPath (Join-Path $full 'BepInEx\core')) -and (Test-Path -LiteralPath (Join-Path $full 'KingdomInc_Data\Managed'))) {
            return $full
        }
    }
    throw "Could not locate validated Kingdom Inc game root (expected BepInEx\core and KingdomInc_Data\Managed). Pass -GameRoot explicitly."
}

$gamePath = Resolve-ValidatedGameRoot $GameRoot
$gamePathForBuild = $gamePath.Replace('\', '/') + '/'
$project = Join-Path $PSScriptRoot 'PathFinder.csproj'

& dotnet build $project -c Release "-p:GameRoot=$gamePathForBuild"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Built: $(Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\PathFinder.dll')"
