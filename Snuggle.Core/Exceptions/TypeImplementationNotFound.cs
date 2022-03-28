using System;

namespace Snuggle.Core.Exceptions;

public class TypeImplementationNotFound : Exception {
    public TypeImplementationNotFound(object classId) : base($"Could not find an implementation for {classId:G}") { }
    public TypeImplementationNotFound(object classId, Exception e) : base($"Could not find an implementation for {classId:G}", e) { }
}
