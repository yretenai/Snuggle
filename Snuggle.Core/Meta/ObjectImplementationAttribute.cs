using System;
using System.Linq;
using JetBrains.Annotations;
using Snuggle.Core.Models;

namespace Snuggle.Core.Meta;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ObjectImplementationAttribute : Attribute {
    public ObjectImplementationAttribute(object classId, UnityGame game = UnityGame.Default) {
        UnderlyingClassId = classId;
        Game = game;
        DisabledGames = Array.Empty<UnityGame>();
    }

    public ObjectImplementationAttribute(object classId, UnityGame[] disabledGames) {
        UnderlyingClassId = classId;
        Game = UnityGame.Default;
        DisabledGames = disabledGames;
    }

    public override object TypeId => (ClassId, Game);

    public UnityGame Game { get; init; }
    public UnityGame[] DisabledGames { get; init; }
    public object UnderlyingClassId { get; init; }
    public UnityClassId ClassId => (UnityClassId) (int) UnderlyingClassId;

    public override int GetHashCode() => HashCode.Combine(ClassId, Game, DisabledGames.GetHashCode());
    public override bool IsDefaultAttribute() => ClassId is UnityClassId.Object && Game is UnityGame.Default && DisabledGames.Length == 0;
    public override string ToString() => $"ObjectImplementationAttribute {{ ClassId = {ClassId:G}, Underlying = {Enum.Format(UnderlyingClassId.GetType(), UnderlyingClassId, "G")}, Game = {Game:G}, Disabled Games = [{string.Join(", ", DisabledGames.Select(x => x.ToString("G")))}] }}";
}
