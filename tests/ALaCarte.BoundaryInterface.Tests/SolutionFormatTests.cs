using ALaCarte.Core.Abstractions;
using ALaCarte.Core.Options;
using ALaCarte.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ALaCarte.BoundaryInterface.Tests;

public class SolutionFormatTests
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly DotNetService _service;

    public SolutionFormatTests()
    {
        _processRunner = Substitute.For<IProcessRunner>();
        _fileSystem = Substitute.For<IFileSystem>();
        var logger = Substitute.For<ILogger<DotNetService>>();
        var dotNetOptions = new DotNetOptions { SolutionName = "Solution" };
        var alacarteOptions = new AlacarteOptions();

        _fileSystem.Combine(Arg.Any<string[]>())
            .Returns(callInfo => string.Join("/", callInfo.Arg<string[]>()));
        _fileSystem.GetFileNameWithoutExtension(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var fileName = path.Split('/').Last();
                var dotIndex = fileName.LastIndexOf('.');
                return dotIndex >= 0 ? fileName[..dotIndex] : fileName;
            });
        _fileSystem.GetDirectoryName(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var path = callInfo.Arg<string>();
                var lastSlash = path.LastIndexOf('/');
                return lastSlash >= 0 ? path[..lastSlash] : null;
            });
        _fileSystem.GetFileName(Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<string>().Split('/').Last());
        _fileSystem.GetRelativePath(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo => callInfo.ArgAt<string>(1));
        _fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>())
            .Returns(Array.Empty<string>());
        _fileSystem.GetDirectories(Arg.Any<string>())
            .Returns(Array.Empty<string>());

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>())
            .Returns(new ProcessResult(0, "", ""));

        var dotNetOpts = Substitute.For<IOptions<DotNetOptions>>();
        dotNetOpts.Value.Returns(dotNetOptions);
        var alacarteOpts = Substitute.For<IOptions<AlacarteOptions>>();
        alacarteOpts.Value.Returns(alacarteOptions);

        _service = new DotNetService(_processRunner, _fileSystem, logger, dotNetOpts, alacarteOpts);
    }

    [Fact]
    public void SolutionFormat_EnumExists()
    {
        Assert.True(typeof(SolutionFormat).IsEnum);
    }

    [Fact]
    public void SolutionFormat_HasAutoSlnAndSlnxValues()
    {
        Assert.True(Enum.IsDefined(typeof(SolutionFormat), "Auto"));
        Assert.True(Enum.IsDefined(typeof(SolutionFormat), "Sln"));
        Assert.True(Enum.IsDefined(typeof(SolutionFormat), "Slnx"));
    }

    [Fact]
    public async Task CreateSolutionAsync_WithFormatSln_PassesFormatSlnArgument()
    {
        await _service.CreateSolutionAsync("/workspace", new List<string>(), format: SolutionFormat.Sln);

        await _processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("new sln") &&
                r.Arguments.Contains("--format sln")));
    }

    [Fact]
    public async Task CreateSolutionAsync_WithFormatSlnx_PassesFormatSlnxArgument()
    {
        await _service.CreateSolutionAsync("/workspace", new List<string>(), format: SolutionFormat.Slnx);

        await _processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("new sln") &&
                r.Arguments.Contains("--format slnx")));
    }

    [Fact]
    public async Task CreateSolutionAsync_WithFormatAuto_DoesNotPassFormatArgument()
    {
        await _service.CreateSolutionAsync("/workspace", new List<string>(), format: SolutionFormat.Auto);

        await _processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("new sln") &&
                !r.Arguments.Contains("--format")));
    }

    [Fact]
    public async Task CreateSolutionAsync_DefaultFormat_IsAuto()
    {
        // Call without specifying format — should behave same as Auto
        await _service.CreateSolutionAsync("/workspace", new List<string>());

        await _processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(r =>
                r.Arguments.Contains("new sln") &&
                !r.Arguments.Contains("--format")));
    }

    [Fact]
    public void IDotNetService_CreateSolutionAsync_HasFormatParameter()
    {
        var method = typeof(IDotNetService).GetMethod("CreateSolutionAsync");
        Assert.NotNull(method);

        var parameters = method.GetParameters();
        var formatParam = parameters.FirstOrDefault(p => p.Name == "format");
        Assert.NotNull(formatParam);
        Assert.Equal(typeof(SolutionFormat), formatParam.ParameterType);
        Assert.True(formatParam.HasDefaultValue);
        Assert.Equal(SolutionFormat.Auto, formatParam.DefaultValue);
    }
}
