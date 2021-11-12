using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mono.Cecil;

namespace Snuggle.Core;

[PublicAPI]
// this is functionally identical to DefaultAssemblyResolver, except that it doesn't consider versions.
public class AssemblyResolver : BaseAssemblyResolver {
    public AssemblyResolver() {
        foreach (var searchDirectory in GetSearchDirectories()) {
            RemoveSearchDirectory(searchDirectory);
        }
    }

    private Dictionary<string, AssemblyDefinition> Cache { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    public override AssemblyDefinition Resolve(AssemblyNameReference name) {
        if (!Cache.TryGetValue(name.Name, out var assembly)) {
            assembly = base.Resolve(name);
            Cache[name.Name] = assembly;
        }

        return assembly;
    }

    public AssemblyDefinition Resolve(string name) {
        if (!Cache.TryGetValue(name, out var assembly)) {
            assembly = base.Resolve(new AssemblyNameReference(name, new Version()));
            Cache[name] = assembly;
        }

        return assembly;
    }

    public bool HasAssembly(string name) => Cache.ContainsKey(name);
    public bool HasAssembly(AssemblyNameReference name) => Cache.ContainsKey(name.Name);

    public void RegisterAssembly(AssemblyDefinition assembly) {
        var name = assembly.Name.Name!;
        if (Cache.ContainsKey(name)) {
            return;
        }

        Cache[name] = assembly;
    }

    protected override void Dispose(bool disposing) {
        Clear();
        base.Dispose(disposing);
    }

    public virtual void Clear() {
        foreach (var (_, assembly) in Cache) {
            assembly.Dispose();
        }

        foreach (var searchDirectory in GetSearchDirectories()) {
            RemoveSearchDirectory(searchDirectory);
        }

        Cache.Clear();
    }
}
