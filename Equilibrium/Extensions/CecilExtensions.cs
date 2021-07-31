using JetBrains.Annotations;
using Mono.Cecil;

namespace Equilibrium.Extensions {
    [PublicAPI]
    public static class CecilExtensions {
        public static bool IsAssignableTo(this TypeDefinition definition, string name) => definition.FullName == name || definition.BaseType != null && definition.BaseType.Resolve().IsAssignableTo(name);

        public static bool IsAssignableTo(this TypeDefinition definition, TypeReference? reference) => reference != null && definition.IsAssignableTo(reference.FullName);
    }
}
