using System;
using Equilibrium.Models;
using JetBrains.Annotations;

namespace Equilibrium.Meta {
    [PublicAPI, AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ObjectImplementationAttribute : Attribute {
        public ObjectImplementationAttribute(ClassId classId) => ClassId = classId;

        public override object TypeId => ClassId;

        public ClassId ClassId { get; init; }

        public override int GetHashCode() => ClassId.GetHashCode();
        public override bool IsDefaultAttribute() => ClassId == ClassId.Object;
        public override string ToString() => $"ObjectImplementationAttribute {{ ClassId = {ClassId:G} }}";
    }
}
