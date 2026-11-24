param(
    [Parameter(Mandatory)]
    [string] $OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$commit = '2ad182a63eabd6d56a6043697c4afd59e846a446'
$project = Split-Path -Parent $PSScriptRoot
$output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
if (Test-Path -LiteralPath $output) {
    throw "Output already exists: $output"
}

# bin is ignored and excluded from the library's source glob.
$scratch = Join-Path $project ("bin/baseline-" + [guid]::NewGuid().ToString('N'))
$source = Join-Path $scratch 'source'
$probe = Join-Path $scratch 'probe'
New-Item -ItemType Directory -Path $scratch | Out-Null
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

try {
    $archive = Join-Path $scratch 'source.zip'
    git -C $project archive --format=zip --output=$archive $commit
    Expand-Archive -LiteralPath $archive -DestinationPath $source
    dotnet build (Join-Path $source 'libMidi.sln') -c Release --disable-build-servers -m:1 "-p:PathMap=$source=/_/libMidi"
    $assembly = Join-Path $source 'bin/Release/net10.0-windows7.0/libMidi.dll'
    $package = Join-Path $source 'bin/Release/libMidi.2.0.0.nupkg'
    $testSource = Join-Path $scratch 'test-source'
    New-Item -ItemType Directory -Path $testSource | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'libMidi.Tests') -File |
        Where-Object Extension -In '.cs', '.csproj' |
        Copy-Item -Destination $testSource
    dotnet build (Join-Path $testSource 'libMidi.Tests.csproj') -c Release --disable-build-servers -m:1 `
        "-p:BaselineAssembly=$assembly" -o $probe

    New-Item -ItemType Directory -Path $output | Out-Null
    $groups = @('T01','T02','T03','T04','T05','T06','T07','T08','T09','T10','T11','T12','T13','T14',
        'known-length','known-system','known-inst-map')
    foreach ($group in $groups) {
        $caseRoot = Join-Path $scratch $group
        $arguments = @((Join-Path $probe 'libMidi.Tests.dll'), $group, $caseRoot)
        if ($group -eq 'T14') {
            $arguments += $package
        }
        dotnet @arguments
        if ($group -eq 'T09') {
            dotnet $arguments[0] T09-read $caseRoot
        }

        foreach ($file in Get-ChildItem -LiteralPath $caseRoot -File | Where-Object Name -Like "$group*") {
            $target = Join-Path $output $file.Name
            if ($file.Extension -eq '.json') {
                $text = [IO.File]::ReadAllText($file.FullName) -replace '\r?\n', "`r`n"
                [IO.File]::WriteAllText($target, $text.TrimEnd() + "`r`n", [Text.UTF8Encoding]::new($false))
            } else {
                [IO.File]::Copy($file.FullName, $target)
            }
        }
    }

    $runtime = Get-Content -LiteralPath (Join-Path $probe 'libMidi.Tests.runtimeconfig.json') -Raw | ConvertFrom-Json
    $metadata = [ordered]@{
        Commit = $commit
        Package = (Split-Path -Leaf $package)
        PackageAssemblySha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $assembly).Hash
        Sdk = (dotnet --version)
        TargetRuntime = $runtime.runtimeOptions.framework.version
        InstalledRuntimes = @(dotnet --list-runtimes | ForEach-Object {
            if ($_ -match '^Microsoft.NETCore.App (10\.[^ ]+) ') { $Matches[1] }
        })
        OS = [Runtime.InteropServices.RuntimeInformation]::OSDescription
        Architecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
        ProbeCulture = 'InvariantCulture'
    }
    $text = ($metadata | ConvertTo-Json) -replace '\r?\n', "`r`n"
    [IO.File]::WriteAllText((Join-Path $output 'environment.json'), $text + "`r`n")
    Write-Host "Baseline written to $output"
} finally {
    $bin = [IO.Path]::GetFullPath((Join-Path $project 'bin')) + [IO.Path]::DirectorySeparatorChar
    $resolved = (Resolve-Path -LiteralPath $scratch).ProviderPath
    if (!$resolved.StartsWith($bin, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unexpected scratch path: $resolved"
    }
    try {
        [IO.Directory]::Delete($resolved, $true)
    } catch [System.IO.IOException] {
        Write-Warning "Could not remove scratch directory: $resolved. $($_.Exception.Message)"
    }
}
