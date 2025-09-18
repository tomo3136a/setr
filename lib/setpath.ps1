param($p="c:\opt\bin", [switch]$pass)

if (test-path $p) {
    Write-Host "path: ${p}" -ForegroundColor Yellow
    $paths=($env:path).Split(";")
    if(-not ($paths|?{$_ -like $p})){
        $path=($paths + @(,$p)) -join ";"
        [Environment]::SetEnvironmentVariable("PATH",$path,1)
    }
}

Write-Host "setpath completed." -ForegroundColor Yellow
if (-not $pass) { $host.UI.RawUI.ReadKey() | Out-Null }
