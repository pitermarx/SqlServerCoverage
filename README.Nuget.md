# SqlServerCoverage
A library and tool to collect SQL coverage data

## Quick start
Assuming powershell
```ps
$cs = "your connection string"
$db = "the database where you want to collect data"

# 1. Install dotnet tool
dotnet tool install sqlservercoverage.commandline -g
# 2. Start coverage session
$id = sql-coverage start --connection-string=$cs --database=$db
# 3. Collect coverage
sql-coverage collect --connection-string=$cs --id=$id --summary
# 4. Cleanup
sql-coverage stop --connection-string=$cs --id=$id
```