rm -Recurse out\build -ErrorAction SilentlyContinue
dotnet build
dotnet publish src\SqlServerCoverage.Commandline --no-build -o out\build