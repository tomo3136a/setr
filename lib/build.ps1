#builder
#
param($OutputPath = "./bin",[switch]$pass)

$AppName = "setr.exe"

$Path = "src/*.cs"
$ReferencedAssemblies = "System.Drawing", "System.Windows.Forms", `
  "System.Xml", "System.Xml.Linq"

Write-Host "build start." -ForegroundColor Yellow

if (-not (Test-Path $OutputPath)) {
  New-Item -Path $OutputPath -ItemType Directory | Out-Null
  Write-Output "make directory."
}
$OutputAssembly = Join-Path (Resolve-Path -Path $OutputPath) $AppName

Write-Output "build program:  $AppName"
Write-Output "    Path:       $Path"
Write-Output "    Output:     $OutputAssembly"
Write-Output "    References: $ReferencedAssemblies"
Add-Type -Path $Path -OutputType ConsoleApplication `
  -OutputAssembly $OutputAssembly `
  -ReferencedAssemblies $ReferencedAssemblies

Copy-Item -Force lib/install.cmd $OutputPath

Write-Host "build completed." -ForegroundColor Yellow
if ($pass) { $host.UI.RawUI.ReadKey() | Out-Null }
