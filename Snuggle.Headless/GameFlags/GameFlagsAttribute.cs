using System;
using Snuggle.Core.Meta;

namespace Snuggle.Headless.GameFlags;

[AttributeUsage(AttributeTargets.Class)]
public class GameFlagsAttribute : Attribute {
    public GameFlagsAttribute(UnityGame game) => Game = game;

    public UnityGame Game { get; }

    public override object TypeId => (int) Game;
    public override bool IsDefaultAttribute() => Game is UnityGame.Default;
}
