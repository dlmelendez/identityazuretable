using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;

var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
