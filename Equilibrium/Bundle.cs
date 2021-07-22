using System.IO;
using Equilibrium.Models.Bundle;
using JetBrains.Annotations;

namespace Equilibrium {
    [PublicAPI]
    public class Bundle {
        public UnityBundle Header { get; init; }
        public IUnityContainer? Container { get; init; }
        
        public Bundle(Stream dataStream, bool leaveOpen = false) {
            using var reader = new BiEndianBinaryReader(dataStream, leaveOpen: leaveOpen);
            
            Header = UnityBundle.FromReader(reader);
            if (Header.Signature == "UnityFS") {
                Container = UnityFS.FromReader(reader, Header);
            }
        }
    }
}
