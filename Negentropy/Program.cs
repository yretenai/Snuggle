using System.Linq;
using Equilibrium;

namespace Negentropy {
    internal static class Program {
        private static void Main(string[] args) {
            var file = new Bundle(args[0], true);
            var data = file.OpenFile(file.Container.Blocks.First().Path);
        }
    }
}
