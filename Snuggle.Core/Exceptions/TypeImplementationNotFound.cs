using System;
using JetBrains.Annotations;

namespace Snuggle.Core.Exceptions;

[PublicAPI]
public class TypeImplementationNotFound : Exception {
    public TypeImplementationNotFound(object classId) : base($"Could not find an implementation for {classId:G}") { }
    public TypeImplementationNotFound(object classId, Exception e) : base($"Could not find an implementation for {classId:G}", e) { }
}
