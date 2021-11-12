using System;
using JetBrains.Annotations;
using Snuggle.Core.Models;

namespace Snuggle.Core.Meta; 

[PublicAPI, AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ObjectImplementationAttribute : Attribute {
    public ObjectImplementationAttribute(object classId, UnityGame game = UnityGame.Default) {
        UnderlyingClassId = classId;
        Game = game;
    }

    public override object TypeId => (ClassId, Game);

    public UnityGame Game { get; init; }
    public object UnderlyingClassId { get; init; }
    public UnityClassId ClassId => (UnityClassId) (int) UnderlyingClassId;

    public override int GetHashCode() => HashCode.Combine(ClassId, Game);
    public override bool IsDefaultAttribute() => ClassId == UnityClassId.Object && Game == UnityGame.Default;
    public override string ToString() => $"ObjectImplementationAttribute {{ ClassId = {ClassId:G}, Underlying = {Enum.Format(UnderlyingClassId.GetType(), UnderlyingClassId, "G")}, Game = {Game:G} }}";
}