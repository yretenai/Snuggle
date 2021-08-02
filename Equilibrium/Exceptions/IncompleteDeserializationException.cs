using System;
using JetBrains.Annotations;

namespace Equilibrium.Exceptions {
    [PublicAPI]
    public class IncompleteDeserializationException : Exception {
        public IncompleteDeserializationException() : base("The class needs to be deserialized first") { }
        public IncompleteDeserializationException(Exception e) : base("The class needs to be deserialized first", e) { }
    }
}
