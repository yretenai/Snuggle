using Snuggle.Core.Implementations;
using Snuggle.Core.Options;

namespace Snuggle.Converters;

public static class SnuggleMeshFile {
    public static void Save(Mesh mesh, string path, ObjectDeserializationOptions deserializationOptions, SnuggleExportOptions exportOptions, SnuggleMeshExportOptions options) { }

    public static void Save(GameObject gameObject, string path, ObjectDeserializationOptions deserializationOptions, SnuggleExportOptions exportOptions, SnuggleMeshExportOptions options) { }
    
    public static GameObject? FindTopGeometry(GameObject? gameObject, bool bubbleUp) {
        while (true) {
            if (gameObject?.Parent.Value == null || bubbleUp) {
                return gameObject;
            }

            gameObject = gameObject.Parent.Value;
        }
    }
}
