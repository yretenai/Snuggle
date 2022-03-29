using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.GameObject)]
public class GameObject : SerializedObject {
    public GameObject(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        var componentCount = reader.ReadInt32();
        Components.EnsureCapacity(componentCount);
        for (var i = 0; i < componentCount; ++i) {
            Components.Add(ComponentPair.FromReader(reader, serializedFile));
        }

        Layer = reader.ReadUInt32();
        Name = reader.ReadString32();
        Tag = reader.ReadUInt16();
        Active = reader.ReadBoolean();
        reader.Align();
    }

    public GameObject(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) => Name = string.Empty;

    public List<ComponentPair> Components { get; set; } = new();
    public uint Layer { get; set; }
    public string Name { get; set; }
    public ushort Tag { get; set; }
    public bool Active { get; set; }

    [JsonIgnore]
    public PPtr<GameObject> Parent {
        get {
            var component = FindComponent(UnityClassId.Transform);
            if (component.IsNull || component.Value is not Transform transform) {
                return PPtr<GameObject>.Null;
            }

            return transform.Parent.Value?.GameObject ?? PPtr<GameObject>.Null;
        }
    }

    [JsonIgnore]
    public List<PPtr<GameObject>> Children {
        get {
            var component = FindComponent(UnityClassId.Transform);
            if (component.IsNull || component.Value is not Transform transform) {
                return new List<PPtr<GameObject>>();
            }

            return transform.Children.Where(x => x.Value != null).Select(x => x.Value!.GameObject).ToList();
        }
    }

    public IEnumerable<PPtr<Component>> FindComponents(params object[] classIds) {
        return Components.Where(x => classIds.Any(y => x.ClassId.Equals(y))).Select(x => x.Ptr);
    }

    public IEnumerable<PPtr<Component>> FindComponents(object classId) {
        return Components.Where(x => x.ClassId.Equals(classId)).Select(x => x.Ptr);
    }

    public PPtr<Component> FindComponent(params object[] classIds) {
        return Components.FirstOrDefault(x => classIds.Any(y => x.ClassId.Equals(y)))?.Ptr ?? PPtr<Component>.Null;
    }

    public PPtr<Component> FindComponent(object classId) {
        return Components.FirstOrDefault(x => x.ClassId.Equals(classId))?.Ptr ?? PPtr<Component>.Null;
    }

    public bool HasComponent(params object[] classIds) {
        return Components.Any(x => classIds.Any(y => x.ClassId.Equals(y)));
    }

    public bool HasComponent(object classId) {
        return Components.Any(x => x.ClassId.Equals(classId));
    }

    public void CacheClassIds() {
        var components = new List<ComponentPair>();
        foreach (var (classId, pPtr) in Components) {
            var value = pPtr.Info?.ClassId ?? classId;

            var tag = default(string);
            if (pPtr.Value is MonoBehaviour monoBehaviour) {
                tag = string.IsNullOrEmpty(monoBehaviour.Name) ? monoBehaviour.Script?.Value?.ToString() : monoBehaviour.Name;
            }

            components.Add(new ComponentPair(value, pPtr) { Tag = tag });
        }

        Components = components;
    }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        base.Serialize(writer, options);
        writer.Write(Components.Count);
        if (options.TargetVersion < UnityVersionRegister.Unity5_5) {
            foreach (var (classId, ptr) in Components) {
                writer.Write((int) classId);
                ptr.ToWriter(writer, SerializedFile, options.TargetVersion);
            }
        } else {
            foreach (var (_, ptr) in Components) {
                ptr.ToWriter(writer, SerializedFile, options.TargetVersion);
            }
        }

        writer.Write(Layer);
        writer.WriteString32(Name);
        writer.Write(Tag);
        writer.Write(Active);
        writer.Align();
    }

    public override int GetHashCode() => HashCode.Combine(Components, Name, Active);
    public override string ToString() => string.IsNullOrEmpty(Name) ? base.ToString() : Name;
}
