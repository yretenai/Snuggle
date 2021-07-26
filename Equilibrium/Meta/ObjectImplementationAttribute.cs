using System;
using Equilibrium.Models;
using JetBrains.Annotations;

namespace Equilibrium.Meta {
    [PublicAPI, AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ObjectImplementationAttribute : Attribute {
        public ObjectImplementationAttribute(ClassId classId, UnityGame game = UnityGame.Default) {
            ClassId = classId;
            Game = game;
        }

        public override object TypeId => (ClassId, Game);

        public UnityGame Game { get; init; }
        public ClassId ClassId { get; init; }

        public override int GetHashCode() => HashCode.Combine(ClassId, Game);
        public override bool IsDefaultAttribute() => ClassId == ClassId.Object && Game == UnityGame.Default;
        public override string ToString() => $"ObjectImplementationAttribute {{ ClassId = {ClassId:G}, Game = {Game:G} }}";
    }
}
