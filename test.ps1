.\build.ps1

# Run unit tests. if there is a failure dont launch the diff tool
$env:DiffEngine_Disabled="true"
dotnet test .\src\SqlServerCoverage.Tests\ --no-build

$connection = "Data Source=(local);Integrated Security=True"
$dbName = "SqlServerCoverageTest"

Write-Host "Ensuring new database"
Invoke-Sqlcmd -ServerInstance "(local)" -Query "if (select DB_ID('$dbName')) is not null
    begin
        alter database [$dbName] set offline with rollback immediate;
        alter database [$dbName] set online;
        drop database [$dbName];
    end";

Invoke-Sqlcmd -ServerInstance "(local)" -Query "CREATE DATABASE [$dbName]";

Write-Host "Starting session"
$id = .\out\build\SqlServerCoverage.CommandLine.exe start --connection-string="$connection" --database=$dbName
if ($LASTEXITCODE -ne 0) { throw $id }

Write-Host "Listing sessions"
$sessions = .\out\build\SqlServerCoverage.CommandLine.exe list --connection-string="$connection"
if (!$sessions.Contains($id)) { throw "Sessions does not contain ID" }

Write-Host "Generating coverage data"
$null = Invoke-Sqlcmd -ServerInstance "(local)" -Database $dbName -Query  "
CREATE PROC TestProcedureForCoverage
    (@value int)
AS
BEGIN
    IF (@value = 1)
        BEGIN
            SELECT 10
        END
    ELSE
        BEGIN
            SELECT 20
        END
END"

$null = Invoke-Sqlcmd -ServerInstance "(local)" -Database $dbName -Query "EXEC dbo.TestProcedureForCoverage 2"

rm -recurse TestResults -ErrorAction SilentlyContinue
.\out\build\SqlServerCoverage.CommandLine.exe collect --connection-string="$connection" --database=$dbName --id=$id --html --opencover --summary --output=out\tests

Write-Host "Closing sessions"
$sessions | % {
    .\out\build\SqlServerCoverage.CommandLine.exe stop --connection-string="$connection" --id=$_
}
$null = Invoke-Sqlcmd -ServerInstance "(local)" -Query "
alter database [$dbName] set offline with rollback immediate;
alter database [$dbName] set online;
drop database [$dbName];"