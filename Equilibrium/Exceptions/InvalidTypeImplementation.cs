using System;
using JetBrains.Annotations;

namespace Equilibrium.Exceptions {
    [PublicAPI]
    public class InvalidTypeImplementation : Exception {
        public InvalidTypeImplementation(object classId) : base($"Implementation for {classId:G} does not inherit SerializedObject") { }
        public InvalidTypeImplementation(object classId, Exception e) : base($"Implementation for {classId:G} does not inherit SerializedObject", e) { }
    }
}
