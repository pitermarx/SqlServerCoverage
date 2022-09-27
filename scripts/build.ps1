$version = .\scripts\get-version.ps1
Write-Host "Building $version"
dotnet build -c Release -p:Version=$version -v=q --nologo
Write-Host "Packing"
dotnet pack src\SqlServerCoverage.Commandline --no-build -c Release -p:Version=$version -v=q --nologo
Write-Host "Installing local tool"
dotnet tool install --local --add-source ./out/tool SqlServerCoverage.CommandLine