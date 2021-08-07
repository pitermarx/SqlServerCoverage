# SqlServerCoverage
A library and tool to collect SQL coverage data

This tool allows us to know how much of the stored procedures are covered by some action

```cs
// Create the initial object to interface into the API
var coverageController = CodeCoverage.NewController(
  "Data Source=(local);Integrated Security=True",
  "DatabaseName");

// Create a new session to collect coverage
var session = coverageController.StartSession();

// Do stufff in the database
...

// Collect coverage data
var results = session.ReadCoverage();

// export to html/opencover
results.Html();
results.OpenCoverXml();

// Clean up stuff
session.StopSession();
```

There are 3 projects, the unit tests, the lib itself and a command line interface. We can use it like this

```powershell
$conn = "Data Source=(local);Integrated Security=True"
#start a session and get the ID
$id = SqlServerCoverage.CommandLine.exe start --connection-string=$conn --database="DatabaseName"
if ($LASTEXITCODE -ne 0) { throw $id }

#collect coverage data
SqlServerCoverage.CommandLine.exe collect `
  --connection-string=$conn --database="Database" --id=$id `
  --html --opencover --summary --output=testresults

#cleanup
SqlServerCoverage.CommandLine.exe stop --connection-string=$conn --id=$id
```

This is a sample summary from the console and attached is a sample HTML report

![Screenshot](screenshots\htmlReport.png)

This is a screenshot of the terminal summary, created with [Spectre.Console](https://spectreconsole.net/)

![Screenshot](screenshots\terminalSummary.png)

The OpenCover xml report also exports the source objects that can then be used by [ReportGenerator](https://danielpalme.github.io/ReportGenerator/) to generate a report