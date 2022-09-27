function Find-NearestVersion($name = 'SqlServerCoverage') {
    $commit =
        git log --oneline |
        Select-String "(.*) $name version (\d*.\d*.\d*)" |
        Select-Object -First 1

    if (!$commit.Matches) {
        return @{
            Version = "1.0.0";
            Hash = git rev-parse HEAD;
        }
    }

    $groups = $commit.Matches[0].Groups
    return @{
        Version = $groups[2].ToString();
        Hash = $groups[1].ToString();
    }
}

$ident = "SqlServerCoverage";
$commit = Find-NearestVersion $ident
$commitsWithIdentSinceVersion = git log --pretty=oneline $commit.Hash | ? { $_ -match $ident }
$build = $commitsWithIdentSinceVersion.Length
$parts = $commit.Version.Split(".")
"$($parts[0]).$($parts[1]).$build"