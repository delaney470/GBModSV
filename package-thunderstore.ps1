param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'),
    [string]$Version = '0.9.0'
)

$ErrorActionPreference = 'Stop'
$stage = Join-Path $OutputDirectory "thunderstore-$Version"
$archive = Join-Path $OutputDirectory "GangBeastsSandevistan-$Version-thunderstore.zip"

Push-Location $PSScriptRoot
try {
    dotnet restore --locked-mode --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }

    dotnet build -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    dotnet run --project tests/Checks.csproj -c Release -- .
    if ($LASTEXITCODE -ne 0) { throw 'Checks failed.' }

    Remove-Item -LiteralPath $stage -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $archive -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path (Join-Path $stage 'Mods') -Force | Out-Null

    Copy-Item 'bin\Release\net6.0\GangBeastsSandevistan.dll' (Join-Path $stage 'Mods\GangBeastsSandevistan.dll')
    Copy-Item 'thunderstore\manifest.json' (Join-Path $stage 'manifest.json')
    Copy-Item 'thunderstore\README.md' (Join-Path $stage 'README.md')
    Copy-Item 'thunderstore\CHANGELOG.md' (Join-Path $stage 'CHANGELOG.md')
    Copy-Item 'thunderstore\icon.png' (Join-Path $stage 'icon.png')

    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive -CompressionLevel Optimal
    Write-Host "Packaged: $archive"
}
finally {
    Pop-Location
}
