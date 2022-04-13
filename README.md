# analyze-dot-net-project

Builds off of Jerrie Pesler's [Analyze Dot Net Project](https://github.com/jerriepelser-blog/AnalyzeDotNetProject). Modifications include:

1. Upgrade to .NET 6
2. Accept arguments via the command line
3. Redirect output to file
4. Filter by dependency name and version

## Usage

```txt
Usage: AnalyzeDotNetProject <projectPath> <outFileName> <dependencyFilter> <versionFilter>

        projectPath             The path the the .NET project to be analyzed

        outFileName             optional: The path for the output. Defaults to 'output.txt'

        dependencyFilter        optional: Filter to projects that have this as a direct or transitive directory

        dependencyVersion       optional: Filter to dependencies that have this version
```
