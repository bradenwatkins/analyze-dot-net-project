using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.ProjectModel;

namespace AnalyzeDotNetProject
{
    class Program
    {
        static void Main(string[] args)
        {
            var projectPath = args.ElementAtOrDefault(0);
            var outFileName = args.ElementAtOrDefault(1) ?? @".\output.txt";
            var dependencyFilter = args.ElementAtOrDefault(2);
            var versionFilter = args.ElementAtOrDefault(3);

            if (string.IsNullOrWhiteSpace(projectPath))
            {
                Console.WriteLine("\nUsage: AnalyzeDotNetProject <projectPath> <outFileName> <dependencyFilter> <versionFilter>\n");
                Console.WriteLine("\tprojectPath\t\tThe path the the .NET project to be analyzed\n");
                Console.WriteLine("\toutFileName\t\toptional: The path for the output. Defaults to 'output.txt'\n");
                Console.WriteLine("\tdependencyFilter\toptional: Filter to projects that have this as a direct or transitive directory\n");
                Console.WriteLine("\tdependencyVersion\toptional: Filter to dependencies that have this version\n");
                return;
            }

            var dependencyGraphService = new DependencyGraphService();
            var dependencyGraph = dependencyGraphService.GenerateDependencyGraph(projectPath);

            var projects = dependencyGraph.Projects.Where(p => p.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference);
            Console.WriteLine($"{projects.Count()} projects found");

            using StreamWriter file = new(outFileName);
            for (var i = 0; i < projects.Count(); i++)
            {
                var project = projects.ElementAt(i);
                Console.WriteLine($"Analyzing project ({i + 1}/{projects.Count()}) - {project.Name}");

                // Generate lock file
                var lockFileService = new LockFileService();
                var lockFile = lockFileService.GetLockFile(project.FilePath, project.RestoreMetadata.OutputPath);

                file.WriteLine(project.Name);

                foreach (var targetFramework in project.TargetFrameworks)
                {
                    file.WriteLine($"  [{targetFramework.FrameworkName}]");

                    var lockFileTargetFramework = lockFile.Targets.FirstOrDefault(t => t.TargetFramework.Equals(targetFramework.FrameworkName));
                    if (lockFileTargetFramework != null)
                    {
                        foreach (var dependency in targetFramework.Dependencies)
                        {
                            var projectLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == dependency.Name);

                            var list = ReportDependency(
                                projectLibrary,
                                lockFileTargetFramework,
                                dependencyFilter,
                                versionFilter,
                                1);

                            if (list.Any())
                            {
                                file.WriteLine(string.Join("\n", list));
                            }
                        }
                    }
                }
            }
        }


        private static IEnumerable<string> ReportDependency(
            LockFileTargetLibrary projectLibrary,
            LockFileTarget lockFileTargetFramework,
            string dependencyFilter,
            string versionFilter,
            int indentLevel)
        {
            var children = Enumerable.Empty<string>();

            foreach (var childDependency in projectLibrary.Dependencies)
            {
                var childLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == childDependency.Id);

                children = children.Concat(ReportDependency(
                    childLibrary,
                    lockFileTargetFramework,
                    dependencyFilter,
                    versionFilter,
                    indentLevel + 1));
            }

            if (children.Any() ||
                ((string.IsNullOrWhiteSpace(dependencyFilter) || projectLibrary.Name.ToLower().Contains(dependencyFilter.ToLower())) &&
                 (string.IsNullOrWhiteSpace(versionFilter) || projectLibrary.Version.ToString().Contains(versionFilter))))
            {
                return children.Prepend($"{new String(' ', indentLevel * 2)}{projectLibrary.Name}, v{projectLibrary.Version}");
            }

            return children;
        }
    }
}
