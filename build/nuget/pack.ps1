$root = (split-path -parent $MyInvocation.MyCommand.Definition) + '\..\..'

$build = $env:APPVEYOR_BUILD_NUMBER
$branch = $env:APPVEYOR_REPO_BRANCH
$pr = $env:APPVEYOR_PULL_REQUEST_NUMBER

$bytes = [System.IO.File]::ReadAllBytes("$root\Skewwhiffy.Batcher\bin\Release\Skewwhiffy.Batcher.dll")
$file = [System.Reflection.Assembly]::Load($bytes)
$fileVersion = $file.GetName().Version
$version = "{0}.{1}.{2}.{3}" -f ($fileVersion.Major, $fileVersion.Minor, $fileVersion.Build, $build)

if ($branch -ne "master") {
  $version += "-{0}" -f ($branch)
}
if ($pr -ne $null) {
  $version += "-pr{0}" -f ($pr)
}

Write-Host "Setting .nuspec version tag to $version"

$content = Get-Content $root\Skewwhiffy.Batcher\Skewwhiffy.Batcher.nuspec
$content = $content -replace '\$version\$', $version
$encoding = New-Object System.Text.UTF8Encoding($false)
$target = "$root\Skewwhiffy.Batcher\Skewwhiffy.Batcher.compiled.nuspec"
[System.IO.File]::WriteAllLines($target, $content, $encoding)

& $root\packages\NuGet.CommandLine.3.4.3\tools\NuGet.exe pack $root\Skewwhiffy.Batcher\Skewwhiffy.Batcher.compiled.nuspec
