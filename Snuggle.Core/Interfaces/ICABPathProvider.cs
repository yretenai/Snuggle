using System.Collections.Generic;
using Snuggle.Core.Implementations;
using Snuggle.Core.Models;

namespace Snuggle.Core.Interfaces;

public interface ICABPathProvider {
    public Dictionary<PPtr<SerializedObject>, string> GetCABPaths();
}
