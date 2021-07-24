using System.IO;
using Equilibrium;
using Equilibrium.IO;

namespace Negentropy {
    internal static class Program {
        private static void Main(string[] args) {
            var serializedFile = new SerializedFile(File.OpenRead(args[0]), args[0], FileStreamHandler.Instance.Value);
        }
    }
}
