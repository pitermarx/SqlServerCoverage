./scripts/build.ps1

# Run unit tests. if there is a failure dont launch the diff tool
Write-Information "Running Unit tests"
$env:DiffEngine_Disabled="true"
dotnet test ./src/SqlServerCoverage.Tests/

$connection = "${env:ConnectionStringForTests};TrustServerCertificate=True"
$dbName = "SqlServerCoverageTests"
$output = "./out/tests"

Write-Host "Ensuring new database"
Invoke-Sqlcmd -ServerInstance ".\SQLEXPRESS" -Query "if (select DB_ID('$dbName')) is not null
    begin
        alter database [$dbName] set offline with rollback immediate;
        alter database [$dbName] set online;
        drop database [$dbName];
    end";

Invoke-Sqlcmd -ServerInstance ".\SQLEXPRESS" -Query "CREATE DATABASE [$dbName]";

Write-Host "Starting sessions"
$id1 = dotnet sql-coverage start --connection-string=$connection --database=$dbName
if ($LASTEXITCODE -ne 0) { throw $id1 }
$id2 = dotnet sql-coverage start --connection-string=$connection --database=$dbName
if ($LASTEXITCODE -ne 0) { throw $id2 }

Write-Host "Listing sessions"
$sessions = dotnet sql-coverage list --connection-string=$connection
if (!$sessions.Contains($id1)) { throw "Sessions does not contain ID1" }
if (!$sessions.Contains($id2)) { throw "Sessions does not contain ID2" }

Write-Host "Stopping all sessions"
dotnet sql-coverage stop-all --connection-string=$connection
$sessions = dotnet sql-coverage list --connection-string=$connection
if ($sessions -ne "No sessions found") { throw "Sessions should have been stopped but were $sessions"}

Write-Host "Starting sessions"
$id = dotnet sql-coverage start --connection-string=$connection --database=$dbName
if ($LASTEXITCODE -ne 0) { throw $id }

Write-Host "Generating coverage data"
$null = (Get-Content ./src/SqlServerCoverage.Tests/test_data.sql -Raw) -Split "GO" | % {
    Invoke-Sqlcmd -ServerInstance ".\SQLEXPRESS" -Database $dbName -Query $_.Trim()
}

dotnet sql-coverage collect `
    --connection-string=$connection --database=$dbName --id=$id `
    --html --opencover --summary --sonar --output=$output

Write-Host "Closing sessions"
dotnet sql-coverage stop --connection-string=$connection --id=$id

$null = Invoke-Sqlcmd -ServerInstance ".\SQLEXPRESS" -Query "
alter database [$dbName] set offline with rollback immediate;
alter database [$dbName] set online;
drop database [$dbName];"

$fileA = "$output/$($dbName)_Coverage.html"
$fileB = "src/SqlServerCoverage.Tests/Snapshots/Tests.TestHtmlOutput.verified.txt"
$diff = Compare-Object $(Get-Content $fileA) $(Get-Content $fileB)
if ($diff) {
    Write-Host "Html file did not output as expected"
    $diff
}

$fileA = "$output/$($dbName)_OpenCover.xml"
$fileB = "src/SqlServerCoverage.Tests/Snapshots/Tests.TestOpenCoverOutput.verified.txt"
$diff = Compare-Object $(Get-Content $fileA) $(Get-Content $fileB)
if ($diff) {
    Write-Host "XML file did not output as expected"
    $diff
}

if (!(Get-Command reportgenerator -EA Ignore)) {
    dotnet tool install dotnet-reportgenerator-globaltool
}

dotnet tool run reportgenerator `
    -reports:$output/$($dbName)_OpenCover.xml `
    -targetdir:$output/opencover `
    -sourcedirs:$output/source `
    -reporttypes:Html