using System;
using JetBrains.Annotations;

namespace Snuggle.Core.Exceptions;

[PublicAPI]
public class IncompleteDeserialization : Exception {
    public IncompleteDeserialization() : base("The class needs to be deserialized first") { }
    public IncompleteDeserialization(Exception e) : base("The class needs to be deserialized first", e) { }
}
