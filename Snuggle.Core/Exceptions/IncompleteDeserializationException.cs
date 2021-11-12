using System;
using JetBrains.Annotations;

namespace Snuggle.Core.Exceptions;

[PublicAPI]
public class IncompleteDeserializationException : Exception {
    public IncompleteDeserializationException() : base("The class needs to be deserialized first") { }
    public IncompleteDeserializationException(Exception e) : base("The class needs to be deserialized first", e) { }
}
