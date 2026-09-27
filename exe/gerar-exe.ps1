[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$buildDirectory = Join-Path $projectRoot "build"
$launcherSource = Join-Path $buildDirectory "Program.cs"
$iconPath = Join-Path $buildDirectory "ocorrencias.ico"
$distDirectory = Join-Path $projectRoot "dist"
$outputExecutable = Join-Path $distDirectory "Ocorrencias.exe"

$compilerCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$compiler = $compilerCandidates |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1

if (-not $compiler) {
    throw "Compilador C# do .NET Framework nao encontrado. Instale as ferramentas de compilacao do .NET para gerar o executavel."
}

$compressionAssembly = Join-Path (Split-Path -Parent $compiler) "System.IO.Compression.dll"
$compressionFileSystemAssembly = Join-Path (Split-Path -Parent $compiler) "System.IO.Compression.FileSystem.dll"
$webExtensionsAssembly = Join-Path (Split-Path -Parent $compiler) "System.Web.Extensions.dll"

$requiredFiles = @(
    (Join-Path $projectRoot "consulta.xlsm"),
    (Join-Path $projectRoot "python\consulta.py"),
    (Join-Path $projectRoot "exemplo\dados_exemplo.csv"),
    (Join-Path $projectRoot "configuracao\ambiente.json"),
    $launcherSource,
    $iconPath,
    $compressionAssembly,
    $compressionFileSystemAssembly,
    $webExtensionsAssembly
)

foreach ($requiredFile in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $requiredFile)) {
        throw "Arquivo obrigatorio nao encontrado: $requiredFile"
    }
}

$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("OcorrenciasBuild_" + [Guid]::NewGuid().ToString("N"))
$payloadDirectory = Join-Path $temporaryRoot "payload"
$payloadZip = Join-Path $temporaryRoot "payload.zip"
$temporaryExecutable = Join-Path $temporaryRoot "Ocorrencias.exe"

try {
    New-Item -ItemType Directory -Force -Path $payloadDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $distDirectory | Out-Null

    Copy-Item -LiteralPath (Join-Path $projectRoot "consulta.xlsm") -Destination $payloadDirectory -Force

    foreach ($directoryName in @("python", "exemplo", "vba", "configuracao")) {
        $sourceDirectory = Join-Path $projectRoot $directoryName
        if (Test-Path -LiteralPath $sourceDirectory) {
            Copy-Item -LiteralPath $sourceDirectory -Destination $payloadDirectory -Recurse -Force
        }
    }

    Compress-Archive -Path (Join-Path $payloadDirectory "*") -DestinationPath $payloadZip -CompressionLevel Optimal

    $compilerArguments = @(
        "/nologo",
        "/target:winexe",
        "/platform:anycpu",
        "/optimize+",
        "/out:$temporaryExecutable",
        "/win32icon:$iconPath",
        "/resource:$payloadZip,Ocorrencias.Payload.zip",
        "/reference:System.dll",
        "/reference:System.Core.dll",
        "/reference:System.Windows.Forms.dll",
        "/reference:$compressionAssembly",
        "/reference:$compressionFileSystemAssembly",
        "/reference:$webExtensionsAssembly",
        $launcherSource
    )

    & $compiler $compilerArguments

    if ($LASTEXITCODE -ne 0) {
        throw "O compilador terminou com o codigo $LASTEXITCODE."
    }

    $hash = $null
    for ($attempt = 1; $attempt -le 10 -and -not $hash; $attempt++) {
        try {
            $hash = Get-FileHash -LiteralPath $temporaryExecutable -Algorithm SHA256 -ErrorAction Stop
        }
        catch {
            if ($attempt -eq 10) {
                throw
            }

            Start-Sleep -Milliseconds 500
        }
    }

    Copy-Item -LiteralPath $temporaryExecutable -Destination $outputExecutable -Force

    Write-Host ""
    Write-Host "Executavel gerado com sucesso:" -ForegroundColor Green
    Write-Host $outputExecutable
    Write-Host "SHA256: $($hash.Hash)"
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
