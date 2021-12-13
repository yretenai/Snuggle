using System;

namespace Snuggle.Core; 

public interface ISingleton<T> where T : class, new() {
    private static Lazy<T> SingletonInstance { get; } = new(() => new T());

    public static T Instance => SingletonInstance.Value;
    public static bool IsCreated => SingletonInstance.IsValueCreated;
}

public class Singleton<T> : ISingleton<T> where T : class, new() {
    public static T Instance => ISingleton<T>.Instance;
    public static bool IsCreated => ISingleton<T>.IsCreated;
}
