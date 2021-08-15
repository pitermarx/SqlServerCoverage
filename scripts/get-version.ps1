function Find-NearestVersion {
    $commit =
        git log --oneline |
        Select-String "(.*) SqlServerCoverage version (\d*.\d*.\d*)" |
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

$commit = Find-NearestVersion
$build = git rev-list --count HEAD "^$($commit.Hash)"
$parts = $commit.Version.Split(".")
"$($parts[0]).$($parts[1]).$build"