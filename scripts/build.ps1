.\scripts\clean.ps1
$version = .\scripts\get-version.ps1
Write-Information "Building"
dotnet build -c Release -p:Version=$version
Write-Information "Packing"
dotnet pack src\SqlServerCoverage.Commandline --no-build -c Release -p:Version=$version
Write-Information "Installing local tool"
dotnet tool install --local --add-source ./out/tool SqlServerCoverage.CommandLine