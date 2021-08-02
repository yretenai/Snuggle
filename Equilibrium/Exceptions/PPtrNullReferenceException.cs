using System;
using JetBrains.Annotations;

namespace Equilibrium.Exceptions {
    [PublicAPI]
    public class PPtrNullReferenceException : Exception {
        public PPtrNullReferenceException(object classId) : base($"PPtr<{classId}> failed to resolve properly") { }
        public PPtrNullReferenceException(object classId, Exception e) : base($"PPtr<{classId}> failed to resolve properly", e) { }
    }
}
