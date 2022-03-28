using System;

namespace Snuggle.Core.Exceptions;

public class IncompleteDeserialization : Exception {
    public IncompleteDeserialization() : base("The class needs to be deserialized first") { }
    public IncompleteDeserialization(Exception e) : base("The class needs to be deserialized first", e) { }
}
