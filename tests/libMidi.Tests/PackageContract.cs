using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;
using libMidi.SMF;

namespace LibMidi.Tests;

internal static class PackageContract
{
    internal static object Describe(string packagePath)
    {
        using ZipArchive package = ZipFile.OpenRead(packagePath);
        using Stream nuspec = package.Entries.Single(entry => entry.Name == "libMidi.nuspec").Open();
        XElement metadata = XDocument.Load(nuspec).Root!.Elements().Single(e => e.Name.LocalName == "metadata");
        XNamespace ns = metadata.Name.Namespace;
        string assemblyPath = typeof(MidiData).Assembly.Location;
        ZipArchiveEntry[] libraries = package.Entries.Where(e => e.FullName.StartsWith("lib/", StringComparison.Ordinal)).ToArray();
        ZipArchiveEntry library = libraries.Single(e => e.Name == "libMidi.dll");
        using Stream binary = library.Open();
        using FileStream builtAssembly = File.OpenRead(assemblyPath);
        return new
        {
            Id = metadata.Element(ns + "id")?.Value,
            Libraries = libraries.Select(e => e.FullName).Order(StringComparer.Ordinal).ToArray(),
            Dependencies = metadata.Element(ns + "dependencies")?.Elements(ns + "group").Select(group => new
            {
                Target = (string?)group.Attribute("targetFramework"),
                Items = group.Elements(ns + "dependency").Select(d => new
                {
                    Id = (string?)d.Attribute("id"),
                    Version = (string?)d.Attribute("version")
                }).OrderBy(d => d.Id, StringComparer.Ordinal).ToArray()
            }).OrderBy(g => g.Target, StringComparer.Ordinal).ToArray(),
            AssemblyMatchesBuild = SHA256.HashData(binary).SequenceEqual(SHA256.HashData(builtAssembly)),
            VersionMatchesBuild = metadata.Element(ns + "version")?.Value ==
                typeof(MidiData).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        };
    }
}
