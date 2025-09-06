$p="c:\opt\bin"
if (test-path $p) {
    $paths=($env:path).Split(";")
    if(-not ($paths|?{$_ -like $p})){
        $path=($paths + @(,$p)) -join ";"
        [Environment]::SetEnvironmentVariable("PATH",$path,1)
    }
}
