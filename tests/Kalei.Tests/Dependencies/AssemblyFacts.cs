using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Kalei.Tests.Dependencies;

/// <summary>What an assembly says about itself, read from metadata without loading it.</summary>
internal sealed record AssemblyFacts(
    IReadOnlySet<string> ReferencedTypes,
    IReadOnlySet<string> ReferencedAssemblies,
    IReadOnlyList<string> NativeCalls)
{
    public static AssemblyFacts Read(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var md = pe.GetMetadataReader();

        var types = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in md.TypeReferences)
        {
            var reference = md.GetTypeReference(handle);
            var ns = md.GetString(reference.Namespace);
            var name = md.GetString(reference.Name);
            types.Add(ns.Length == 0 ? name : $"{ns}.{name}");
        }

        var assemblies = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in md.AssemblyReferences)
        {
            assemblies.Add(md.GetString(md.GetAssemblyReference(handle).Name));
        }

        var nativeCalls = new List<string>();
        foreach (var handle in md.MethodDefinitions)
        {
            var method = md.GetMethodDefinition(handle);
            if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0)
            {
                nativeCalls.Add(md.GetString(method.Name));
            }
        }

        return new AssemblyFacts(types, assemblies, nativeCalls);
    }
}
