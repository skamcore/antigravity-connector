param([string]$Destination = 'outputs\AntigravityConnect-Setup.exe', [switch]$Audit)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'C# compiler not found.' }
Push-Location $projectRoot
try {
    $sources = @('Core.cs', 'App.cs', 'UI.cs', 'I18n.cs', 'Tests.cs', 'AssemblyInfo.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
    $references = @('/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll', '/reference:System.Web.Extensions.dll', '/reference:Microsoft.CSharp.dll')
    $resource = '/resource:' + (Join-Path $PSScriptRoot 'app.ico') + ',Connector.ico'
    if ($Audit) {
        $auditPath = Join-Path $PSScriptRoot 'audit\Audit.exe'
        & $compiler /nologo /target:exe /main:AuditRunner /optimize+ /utf8output /codepage:65001 $resource ('/out:' + $auditPath) @references @sources (Join-Path $PSScriptRoot 'audit\AuditRunner.cs')
        if ($LASTEXITCODE -ne 0) { throw 'Audit build failed.' }
        & $auditPath
        if ($LASTEXITCODE -ne 0) { throw 'Audit tests failed.' }
    }
    $outputPath = [IO.Path]::GetFullPath($Destination)
    New-Item -ItemType Directory -Path (Split-Path -Parent $outputPath) -Force | Out-Null
    & $compiler /nologo /target:winexe /optimize+ /utf8output /codepage:65001 ('/win32icon:' + (Join-Path $PSScriptRoot 'app.ico')) $resource ('/out:' + $outputPath) @references @sources
    if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
    Write-Output ('Built: ' + $outputPath)
} finally { Pop-Location }
