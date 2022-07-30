using System;
using System.Collections.Generic;

namespace Snuggle.Core.Extensions;

public static class ListExtensions {
    public static void AddRange<T>(this List<T> list, Span<T> span) {
        list.EnsureCapacity(span.Length);
        foreach (var element in span) {
            list.Add(element);
        }
    }
}
