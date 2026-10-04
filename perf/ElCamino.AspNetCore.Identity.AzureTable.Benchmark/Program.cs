using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

// BenchmarkDotNet builds the benchmarks again in a project of its own, without the properties given to this build.
// They are passed on, otherwise the benchmarks always run against the local project and never the NuGet package.
Argument[] libraryReferenceArguments = [.. typeof(Program).Assembly
    .GetCustomAttributes<AssemblyMetadataAttribute>()
    .Where(metadata => metadata.Key is "UseLocalProject" or "BenchmarkPackageVersion")
    .Select(metadata => new MsBuildArgument($"/p:{metadata.Key}={metadata.Value}"))];

var singleProcessorJob = Job.Default
    .WithAffinity(new IntPtr(1))
    .WithEnvironmentVariable("DOTNET_PROCESSOR_COUNT", "1")
    .WithArguments(libraryReferenceArguments);

var config = DefaultConfig.Instance
    .WithOptions(ConfigOptions.DisableOptimizationsValidator)
    .AddJob(singleProcessorJob);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
