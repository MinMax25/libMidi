using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace LibMidi.Tests;

internal static class TestEnvironment
{
    internal const string ParentArchitectureVariable = "LIBMIDI_PARENT_ARCH";

    internal static void VerifyAndRecord(string role)
    {
        string os = OperatingSystem.IsWindows() ? "Windows"
            : OperatingSystem.IsLinux() ? "Linux"
            : OperatingSystem.IsMacOS() ? "macOS"
            : "Other";
        string osArchitecture = RuntimeInformation.OSArchitecture.ToString();
        string processArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
        string report = JsonSerializer.Serialize(new
        {
            Role = role,
            OS = os,
            RuntimeInformation.OSDescription,
            OSArchitecture = osArchitecture,
            ProcessArchitecture = processArchitecture,
            RuntimeInformation.FrameworkDescription,
            Culture = CultureInfo.CurrentCulture.Name,
            UICulture = CultureInfo.CurrentUICulture.Name
        });

        Console.WriteLine(report);
        string? log = Environment.GetEnvironmentVariable("LIBMIDI_ENVIRONMENT_LOG");
        if (!string.IsNullOrEmpty(log))
        {
            File.AppendAllText(log, report + "\n");
        }

        Verify("LIBMIDI_EXPECTED_OS", os, "OS");
        Verify("LIBMIDI_EXPECTED_ARCH", osArchitecture, "OS architecture");
        Verify("LIBMIDI_EXPECTED_ARCH", processArchitecture, "process architecture");
        Verify(ParentArchitectureVariable, processArchitecture, "child process architecture");
    }

    private static void Verify(string variable, string actual, string label)
    {
        string? expected = Environment.GetEnvironmentVariable(variable);
        if (!string.IsNullOrEmpty(expected) && !string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Expected {label} '{expected}', observed '{actual}'.");
        }
    }
}
