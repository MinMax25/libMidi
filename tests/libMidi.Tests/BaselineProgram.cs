using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using libMidi.SMF;

namespace LibMidi.Tests;

internal static class BaselineProgram
{
    internal static readonly string[] Cases =
    [
        "T01", "T02", "T03", "T04", "T05", "T06", "T07", "T08", "T09", "T10", "T11", "T12", "T13",
        "known-length", "known-system", "known-inst-map"
    ];

    internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Main(string[] args)
    {
        const string cultureOption = "--culture=";
        int positionalCount = args.FirstOrDefault() == "T14" ? 3 : 2;
        bool hasCulture = args.Length == positionalCount + 1 && args[^1].StartsWith(cultureOption, StringComparison.Ordinal);
        if (args.Length != positionalCount && !hasCulture)
        {
            Console.Error.WriteLine("Usage: libMidi.Tests.dll <case> <output directory> [package for T14] [--culture=<name>]");
            return 2;
        }

        CultureInfo culture = hasCulture ? CultureInfo.GetCultureInfo(args[^1][cultureOption.Length..]) : CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        string root = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(root);
        bool defaultRootIsAppBase = ConverterSetting.Root == AppContext.BaseDirectory;
        ConverterSetting.Root = Path.Combine(root, "作業 フォルダー");
        if (args[0] is not ("T13" or "T13-current"))
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        try
        {
            TestEnvironment.VerifyAndRecord(args[0]);
            object result = args[0] switch
            {
                "T09" => new { DefaultRootIsAppBase = defaultRootIsAppBase, Config = BehaviorCases.Run("T09", root) },
                "T14" => PackageContract.Describe(args[2]),
                _ => BehaviorCases.Run(args[0], root)
            };
            File.WriteAllText(Path.Combine(root, args[0] + ".json"),
                JsonSerializer.Serialize(result, JsonOptions));
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    internal static async Task RunChildAsync(string name, string root, string? culture = null)
    {
        var start = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(typeof(BaselineProgram).Assembly.Location);
        start.ArgumentList.Add(name);
        start.ArgumentList.Add(root);
        if (culture != null)
        {
            start.ArgumentList.Add($"--culture={culture}");
        }

        start.Environment[TestEnvironment.ParentArchitectureVariable] = RuntimeInformation.ProcessArchitecture.ToString();
        using Process child = Process.Start(start) ?? throw new InvalidOperationException("Probe did not start.");
        Task<string> error = child.StandardError.ReadToEndAsync();
        Task<string> output = child.StandardOutput.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await child.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            child.Kill(entireProcessTree: true);
            throw;
        }

        if (child.ExitCode != 0)
        {
            throw new InvalidOperationException($"{name} exited with {child.ExitCode}\n{await error}\n{await output}");
        }
    }
}
