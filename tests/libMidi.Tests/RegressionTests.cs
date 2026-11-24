using System.IO.Compression;
using System.Reflection;
using System.Runtime.Versioning;
using System.Runtime.CompilerServices;
using System.Text.Json;
using libMidi.SMF;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LibMidi.Tests;

public sealed class RegressionTests
{
    static RegressionTests()
    {
        TestEnvironment.VerifyAndRecord("test host");
    }

    public static IEnumerable<object[]> CultureCases =>
        from culture in new[] { "ja-JP", "en-US", "de-DE" }
        from scenario in new[] { "T02", "T03", "T06", "T07", "T08", "T09", "T10", "T11", "T13" }
        select new object[] { culture, scenario };

    public static IEnumerable<object[]> BaselineCases =>
        BaselineProgram.Cases.Where(name => !IsKnown(name) && name is not ("T11" or "T13")).Select(name => new object[] { name });

    public static IEnumerable<object[]> KnownCases =>
        BaselineProgram.Cases.Where(IsKnown).Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(BaselineCases))]
    [Trait("Kind", "Regression")]
    public Task MatchesV200(string name) => CompareCaseAsync(name);

    [Theory]
    [MemberData(nameof(KnownCases))]
    [Trait("Kind", "KnownBehavior")]
    public Task RecordsV200Limitation(string name) => CompareCaseAsync(name);

    [Theory]
    [MemberData(nameof(CultureCases))]
    [Trait("Kind", "Culture")]
    public Task MatchesUnderCulture(string culture, string scenario) =>
        CompareCaseAsync(scenario, culture: culture);

    [Fact]
    [Trait("Kind", "IntentionalChange")]
    public Task T11_UsesSafeTrackNamesAsync() =>
        CompareCaseAsync("T11");

    [Fact]
    [Trait("Kind", "IntentionalChange")]
    public Task T13_CodePagesWorkWithoutGlobalRegistrationAsync() =>
        CompareCaseAsync("T13");

    [Fact]
    [Trait("Kind", "IntentionalChange")]
    public void T14_PackageTargetsNet10AndPreservesContract()
    {
        var assembly = typeof(MidiData).Assembly;
        string configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
        string project = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(SourcePath())!, "..", ".."));
        string? platform = typeof(RegressionTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "LibMidiBuildPlatform").Value;
        string output = platform == "x64" ? Path.Combine("bin", "x64", configuration) : Path.Combine("bin", configuration);
        string package = Path.Combine(project, output, $"libMidi.{version}.nupkg");
        object contract = PackageContract.Describe(package);
        string baseline = File.ReadAllText(ExpectedPath("T14.json"));
        // The library path and dependency-group TFM are the only intended contract differences.
        Assert.Equal(baseline.Replace("net10.0-windows7.0", "net10.0", StringComparison.Ordinal),
            JsonSerializer.Serialize(contract, BaselineProgram.JsonOptions) + "\n", ignoreLineEndingDifferences: true);
        Assert.Equal(".NETCoreApp,Version=v10.0", assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName);
        Assert.Empty(assembly.GetCustomAttributes<SupportedOSPlatformAttribute>());

        using ZipArchive archive = ZipFile.OpenRead(package);
        Assert.All(archive.Entries, entry => Assert.True(IsPackageEntryExpected(entry.FullName), entry.FullName));
        ZipArchiveEntry readme = Assert.IsType<ZipArchiveEntry>(archive.GetEntry("README.md"));
        using Stream contents = readme.Open();
        using var copy = new MemoryStream();
        contents.CopyTo(copy);
        Assert.Equal(File.ReadAllBytes(Path.Combine(project, "README.md")), copy.ToArray());
    }

    private static bool IsPackageEntryExpected(string name) =>
        name is "[Content_Types].xml" or "_rels/.rels" or "README.md" or "libMidi.nuspec" or "lib/net10.0/libMidi.dll" ||
        (name.StartsWith("package/services/metadata/core-properties/", StringComparison.Ordinal) &&
         name.EndsWith(".psmdcp", StringComparison.Ordinal));

    private static string SourcePath([CallerFilePath] string path = "") => path;

    private static bool IsKnown(string name) => name.StartsWith("known-", StringComparison.Ordinal);

    private static async Task CompareCaseAsync(string name, string? culture = null)
    {
        DirectoryInfo temporary = Directory.CreateTempSubdirectory("libMidi-regression-");
        try
        {
            string probeName = name is "T11" or "T13" ? name + "-current" : name;
            await BaselineProgram.RunChildAsync(probeName, temporary.FullName, culture);
            if (name == "T09")
            {
                await BaselineProgram.RunChildAsync("T09-read", temporary.FullName, culture);
                AssertJson(ExpectedPath("T09-read.json"), File.ReadAllText(Path.Combine(temporary.FullName, "T09-read.json")));
            }

            string actual = File.ReadAllText(Path.Combine(temporary.FullName, probeName + ".json"));
            if (name == "T11")
            {
                using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath("T11.json")));
                using var observed = JsonDocument.Parse(actual);
                Assert.True(JsonElement.DeepEquals(expected.RootElement.GetProperty("Normal"), observed.RootElement), actual);
            }
            else if (name != "T13")
            {
                AssertJson(ExpectedPath(name + ".json"), actual);
            }
            foreach (string file in BinaryOutputs(name))
            {
                Assert.Equal(File.ReadAllBytes(ExpectedPath(file)), File.ReadAllBytes(Path.Combine(temporary.FullName, file)));
            }
        }
        finally
        {
            temporary.Delete(recursive: true);
        }
    }

    private static string[] BinaryOutputs(string name) => name switch
    {
        "T02" => ["T02-filtered.mid", "T02-full.mid"],
        "T07" => ["T07-SJIS.mid", "T07-UTF8.mid"],
        "T10" => ["T10-clean.srt", "T10-comments.srt", "T10-export_Lyric.srt", "T10-export_Lyric.txt"],
        "T11" => ["T11-split.mid"],
        _ => []
    };

    private static void AssertJson(string expected, string actual)
    {
        // Ignore checkout line endings around JSON tokens. Embedded text and binary outputs remain exact.
        Assert.Equal(File.ReadAllText(expected), actual + "\n", ignoreLineEndingDifferences: true);
    }

    private static string ExpectedPath(string name) => Path.Combine(AppContext.BaseDirectory, "Baseline", name);
}
