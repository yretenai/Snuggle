using System;
using JetBrains.Annotations;

namespace Snuggle.Core.Exceptions;

[PublicAPI]
public class PPtrNullReferenceException : Exception {
    public PPtrNullReferenceException(object classId) : base($"PPtr<{classId:G}> failed to resolve properly") { }
    public PPtrNullReferenceException(object classId, Exception e) : base($"PPtr<{classId:G}> failed to resolve properly", e) { }
}
