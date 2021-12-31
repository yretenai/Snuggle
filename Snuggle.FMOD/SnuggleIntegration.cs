using System.Reflection;
using Snuggle.Native;

namespace Snuggle.FMOD;

public static class SnuggleIntegration {
    public static void Register() {
        Helper.Register(Assembly.GetExecutingAssembly());
    }
}
