param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'),
    [string]$Version = '0.9.0-beta.1'
)

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $OutputDirectory = (Resolve-Path $OutputDirectory).Path
    $stage = Join-Path $OutputDirectory ("GangBeastsSandevistan-$Version")
    New-Item -ItemType Directory -Force -Path (Join-Path $stage 'Mods') | Out-Null

    dotnet restore --locked-mode --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw 'Dependency restore failed.' }
    dotnet build -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    dotnet run --project tests/Checks.csproj -c Release -- .
    if ($LASTEXITCODE -ne 0) { throw 'Release checks failed.' }

    Copy-Item 'bin/Release/net6.0/GangBeastsSandevistan.dll' (Join-Path $stage 'Mods')
    Copy-Item 'README.md','CHANGELOG.md','KNOWN_ISSUES.md','LICENSE' $stage

    $zip = Join-Path $OutputDirectory "GangBeastsSandevistan-$Version.zip"
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
    $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
    Set-Content -NoNewline -Path "$zip.sha256" -Value "$hash  $(Split-Path $zip -Leaf)"
    Write-Output "Packaged: $zip"
    Write-Output "SHA256: $hash"
}
finally { Pop-Location }
