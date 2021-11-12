using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Snuggle.Core.Extensions;

[PublicAPI]
public static class ListExtensions {
    public static void AddRange<T>(this List<T> list, Span<T> span) {
        list.AddRange(ref span);
    }

    public static void AddRange<T>(this List<T> list, ref Span<T> span) {
        list.EnsureCapacity(span.Length);
        foreach (var element in span) {
            list.Add(element);
        }
    }
}
