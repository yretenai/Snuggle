using System.IO;
using System.Linq;
using Equilibrium;
using Equilibrium.IO;

namespace Negentropy {
    internal static class Program {
        private static void Main(string[] args) {
            var bundle = Bundle.OpenBundleSequence(File.OpenRead(args[0]), args[0]);
            var test = bundle[0].OpenFile(bundle[0].Container.Blocks.First().Path);
            var serialized = new SerializedFile(new MemoryStream(test) { Position = 0 }, bundle[0].Container.Blocks.First().Path, new BundleStreamHandler(bundle[0]));
        }
    }
}
