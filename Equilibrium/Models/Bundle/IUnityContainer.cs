using System.Collections.Generic;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public interface IUnityContainer {
        public ICollection<UnityBundleBlockInfo>? BlockInfos { get; set; }
        public ICollection<UnityBundleBlock>? Blocks { get; set; }
    }
}
