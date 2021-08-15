Write-Information "Deleting old binaries"
Get-ChildItem bin -Recurse | rm -Recurse -Force
Get-ChildItem obj -Recurse | rm -Recurse -Force
Get-ChildItem out -ErrorAction SilentlyContinue | rm -Recurse -Force

Write-Information "Clearing nuget cache for tool reinstall after build"
# Otherwise we would keep installing the same tool even though we have a fresh build
# Another solution would be to increment the version on each build
dotnet nuget locals all -l |
    % { $_.Substring($_.IndexOf(" ")).Trim() } |
    % { gci "$_\sqlservercoverage.commandline" -ErrorAction SilentlyContinue | rm -r }

Write-Information "Uninstalling dotnet tool"
$null = dotnet tool uninstall sqlservercoverage.commandline