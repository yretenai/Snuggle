using System.Collections.Generic;
using Equilibrium.Implementations;
using Equilibrium.Models;

namespace Equilibrium.Interfaces {
    public interface ICABPathProvider {
        public Dictionary<string, PPtr<SerializedObject>> GetCABPaths();
    }
}
