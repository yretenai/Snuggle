using System.ComponentModel;
using JetBrains.Annotations;

namespace Entropy.Handlers {
    [PublicAPI]
    public enum MaterialPrimaryColor {
        Amber,
        Blue,

        [Description("Blue Grey")]
        BlueGrey,
        Brown,
        Cyan,

        [Description("Deep Orange")]
        DeepOrange,

        [Description("Deep Purple")]
        DeepPurple,
        Green,
        Grey,
        Indigo,

        [Description("Light Blue")]
        LightBlue,

        [Description("Light Green")]
        LightGreen,
        Lime,
        Orange,
        Pink,
        Purple,
        Red,
        Teal,
        Yellow,
    }
}
