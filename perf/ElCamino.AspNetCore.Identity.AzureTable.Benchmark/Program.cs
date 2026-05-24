using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

var singleProcessorJob = Job.Default
	.WithAffinity(new IntPtr(1))
	.WithEnvironmentVariable("DOTNET_PROCESSOR_COUNT", "1");

var config = DefaultConfig.Instance
	.WithOptions(ConfigOptions.DisableOptimizationsValidator)
	.AddJob(singleProcessorJob);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
