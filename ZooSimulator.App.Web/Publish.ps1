param(
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$sourceDirectory = Split-Path $PSScriptRoot -Parent

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputDirectory = Join-Path $sourceDirectory "artifacts/web/$stamp"
}

$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)

# Fresh folders avoid stale fingerprinted runtime files from an earlier publish.
if (Test-Path -LiteralPath $outputPath) {
    throw "Choose a new output directory; it already exists: $outputPath"
}

New-Item -ItemType Directory -Path $outputPath | Out-Null
$publishPath = Join-Path $outputPath 'publish'
$projectPath = Join-Path $PSScriptRoot 'ZooSimulator.App.Web.csproj'

# Ordinary IDE builds share the standard Engine reference. Only this isolated
# publish graph builds the asset-free Engine variant for browser distribution.
& dotnet publish $projectPath -c Release -p:EngineEmbedAssets=false "-p:PublishDir=$publishPath/" --nologo

if ($LASTEXITCODE -ne 0) {
    throw 'Browser publish failed; no upload package was produced.'
}

$gamePath = Join-Path $outputPath 'ZooSimulator'
Copy-Item -LiteralPath (Join-Path $publishPath 'wwwroot') -Destination $gamePath -Recurse
Copy-Item -LiteralPath (Join-Path $sourceDirectory 'THIRD-PARTY-NOTICES.md') -Destination $gamePath

# Collect the original notices for the runtime actually resolved by this build.
$assets = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'obj/project.assets.json') | ConvertFrom-Json
$runtime = $assets.project.frameworks.PSObject.Properties.Value.downloadDependencies |
    Where-Object name -EQ 'Microsoft.NETCore.App.Runtime.Mono.browser-wasm' |
    Select-Object -First 1

if ($null -eq $runtime) {
    throw 'Cannot find the browser runtime package for licence collection.'
}

$runtimeVersion = $runtime.version.Trim('[', ']').Split(',')[0].Trim()
$runtimeRelativePath = $runtime.name.ToLowerInvariant() + '/' + $runtimeVersion
$packageRoot = $assets.packageFolders.PSObject.Properties.Name | Where-Object {
    Test-Path -LiteralPath (Join-Path $_ $runtimeRelativePath)
} | Select-Object -First 1

if ($null -eq $packageRoot) {
    throw 'Cannot locate the resolved browser runtime package on disk.'
}

$runtimePath = Join-Path $packageRoot $runtimeRelativePath
Copy-Item -LiteralPath (Join-Path $runtimePath 'LICENSE.TXT') -Destination (Join-Path $gamePath 'licenses/dotnet-LICENSE.txt')
Copy-Item -LiteralPath (Join-Path $runtimePath 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $gamePath 'licenses/dotnet-THIRD-PARTY-NOTICES.txt')

@'
Upload instructions

In WinSCP, open /public_html/ZooSimulator/ on the server.
Open the local ZooSimulator folder beside this file.
Upload ALL ITS CONTENTS, including .htaccess, _framework, models, vendor, Brand and licenses.
index.html must be directly inside /public_html/ZooSimulator/ (no extra nested ZooSimulator folder).
Open https://seanstar.co.uk/ZooSimulator/ and allow the first download to finish.
Do not open index.html directly from disk. No PHP, Node or .NET server installation is needed.
Each page load starts a fresh game. Background tabs follow browser timer scheduling; there is no catch-up.

If you see HTTP 500, rename only this game's .htaccess to .htaccess-disabled and retry.
If loading fails, send the displayed error and browser console/network error. Do not send credentials.
On updates, replace the complete game contents and hard-refresh the page.

The ZIP is an alternative: extract it locally and upload its contents.
Do not merely upload the ZIP and expect the server to extract it.
'@ | Set-Content -LiteralPath (Join-Path $outputPath 'UPLOAD-INSTRUCTIONS.txt') -Encoding utf8

# ZipFile includes the dotfile, unlike some archive-tool defaults.
$zipPath = Join-Path $outputPath 'ZooSimulator-upload.zip'
[System.IO.Compression.ZipFile]::CreateFromDirectory($gamePath, $zipPath)

Write-Output "Upload folder: $gamePath"
Write-Output "ZIP alternative: $zipPath"
