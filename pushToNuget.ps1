$branch = $env:APPVEYOR_REPO_BRANCH;
if ($branch -eq "master") {
  $pr_number = $env:APPVEYOR_PULL_REQUEST_NUMBER
  if ($pr_number -eq $null) {
    echo "Pushing to NUGET QQQQ : $pr_number"
  }
  else {
    echo "Build is on a PR, so skipping Nuget publish"
  }
}
else {
  echo "Build is on branch $branch, not master, so skipping Nuget publish"
}
