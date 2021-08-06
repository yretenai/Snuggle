using System;
using JetBrains.Annotations;

namespace Equilibrium.Exceptions {
    [PublicAPI]
    public class TypeImplementationNotFoundException : Exception {
        public TypeImplementationNotFoundException(object classId) : base($"Could not find an implementation for {classId:G}") { }
        public TypeImplementationNotFoundException(object classId, Exception e) : base($"Could not find an implementation for {classId:G}", e) { }
    }
}
