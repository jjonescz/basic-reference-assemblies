using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Basic.Reference.Assemblies.UnitTests;

public class SanityUnitTests(ITestOutputHelper outputHelper)
{
    public ITestOutputHelper TestOutputHelper { get; } = outputHelper;

    [Theory]
    [MemberData(nameof(TestData.ApplicationReferences), MemberType = typeof(TestData))]
    public void AllAppCanCompile(string targetFramework, IEnumerable<MetadataReference> references)
    {
        TestOutputHelper.WriteLine(targetFramework);
        var source = """
            using System;

            public class C
            {
                public Exception Exception;

                static void Main() 
                {
                    Console.WriteLine("Hello World");
                }
            }
            """;

        var compilation = CSharpCompilation.Create(
            "Example",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references: references);

        Assert.Empty(compilation.GetDiagnostics());
        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        Assert.True(emitResult.Success);
        Assert.Empty(emitResult.Diagnostics);
        Assert.NotEmpty(targetFramework);
    }

    [Theory]
    [MemberData(nameof(TestData.AllReferences), MemberType = typeof(TestData))]
    public void AllLibraryCanCompile(string targetFramework, IEnumerable<MetadataReference> references)
    {
        var source = @"
using System;

public class C
{
public Exception Exception;

static void Main() 
{
    Console.WriteLine(""Hello World"");
}
}";

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create(
            "Example",
            new[] { CSharpSyntaxTree.ParseText(source) },
            options: options,
            references: references);

        // NetStandard1.3 comes with several no warn options we need here
        if (targetFramework == "netstandard1.3")
        {
            compilation = NoWarn(compilation, "CS1702");
            compilation = NoWarn(compilation, "CS1701");
        }

        Assert.Empty(compilation.GetDiagnostics());
        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        Assert.True(emitResult.Success);
        Assert.Empty(emitResult.Diagnostics);
    }

    private static CSharpCompilation NoWarn(CSharpCompilation compilation, string id)
    {
        var diagnosticMap = compilation.Options.SpecificDiagnosticOptions;
        diagnosticMap = diagnosticMap.SetItem(id, ReportDiagnostic.Suppress);
        var options = compilation.Options.WithSpecificDiagnosticOptions(diagnosticMap);
        return compilation.WithOptions(options);
    }

    [Theory]
    [MemberData(nameof(TestData.AllValues), MemberType = typeof(TestData))]
    public void AllReferenceInfo(string targetFramework, IEnumerable<(string FileName, byte[] ImageBytes, PortableExecutableReference Reference, Guid Mvid)> referenceValues)
    {
        var list = referenceValues.ToList();
        Assert.True(list.Count > 35, $"Not enough assemblies in {targetFramework}"); // Make sure we have it all
        foreach (var tuple in list)
        {
            Assert.Equal(tuple.Mvid, tuple.Reference.GetMvid());
            Assert.NotNull(tuple.Reference);
        }
    }

    [Fact]
    public void ReferencesLazyInit()
    {
        Assert.NotEmpty(NetCoreApp31.References.All);
        Assert.NotEmpty(Net50.References.All);
        Assert.NotEmpty(Net70.References.All);
        Assert.NotEmpty(Net80.References.All);
        Assert.NotEmpty(NetStandard13.References.All);
        Assert.NotEmpty(Net461.References.All);
        Assert.NotEmpty(Net472.References.All);
    }
}
