using System;
using System.IO;
using System.Text;
using MelonLoader;
using UnhollowerBaseLib;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using Type = Il2CppSystem.Type;

namespace Snuggle.SubScenes {
    public static class BuildInfo {
        public const string Name = "Snuggle.SubScenes.TypeInfoDumper";
        public const string Description = "Dumps Unity Entity Info";
        public const string Author = "Legiayayana";
        public const string Company = null;
        public const string Version = "1.0.0";
        public const string DownloadLink = null;
    }

    public class TypeInfoDumper : MelonMod {
        // hacky workaround because unhollower tries to allocate a T*, which is invalid.
        // this is probably leaky
        // see: https://github.com/knah/Il2CppAssemblyUnhollower/issues/88
        private static unsafe IntPtr GetNativeArrayIntPtr<T>(Il2CppObjectBase array) where T : new() {
            var ptr = IL2CPP.Il2CppObjectBaseToPtrNotNull(array) + (int) IL2CPP.il2cpp_field_get_offset(IL2CPP.GetIl2CppField(Il2CppClassPointerStore<NativeList<T>>.NativeClassPtr, "m_ListData"));
            var intPtr = *(IntPtr*) ptr;
            var unsafeList = *(UnsafeList*) intPtr;
            return unsafeList.Ptr;
        }

        public override unsafe void OnUpdate() {
            if (Input.GetKeyDown(KeyCode.F6)) {
                MelonLogger.Msg("Getting Types");
                var allTypes = TypeManager.GetAllTypes();

                MelonLogger.Msg("Saving");
                var entityOffsets = (TypeManager.EntityOffsetInfo*) GetNativeArrayIntPtr<TypeManager.EntityOffsetInfo>(TypeManager.s_EntityOffsetList);
                var blobOffsets = (TypeManager.EntityOffsetInfo*) GetNativeArrayIntPtr<TypeManager.EntityOffsetInfo>(TypeManager.s_BlobAssetRefOffsetList);
                var stream = File.OpenWrite(MelonUtils.BaseDirectory + "/" + "TypeInfo.bin");
                var writer = new BinaryWriter(stream);
                writer.Write(1);
                writer.Write(allTypes.Length);
                foreach (var type in allTypes) {
                    writer.Write(type.StableTypeHash);
                    writer.Write(type.MemoryOrdering);
                    writer.Write(type.AlignmentInBytes);
                    writer.Write(type.SizeInChunk);
                    writer.Write(type.ElementSize);
                    writer.Write(type.TypeSize);
                    writer.Write(type.BufferCapacity);
                    writer.Write(type.MaximumChunkCapacity);
                    writer.Write((int) type.Category);
                    writer.Write(type.EntityOffsetCount);
                    if (type.EntityOffsetCount != 0) {
                        for (var i = 0; i < type.EntityOffsetCount; ++i) {
                            writer.Write(entityOffsets[type.EntityOffsetStartIndex + i].Offset);
                        }
                    }

                    writer.Write(type.BlobAssetRefOffsetCount);
                    if (type.BlobAssetRefOffsetCount != 0) {
                        for (var i = 0; i < type.BlobAssetRefOffsetCount; ++i) {
                            writer.Write(blobOffsets[type.BlobAssetRefOffsetStartIndex + i].Offset);
                        }
                    }

                    WriteType(writer, TypeManager.GetType(type.TypeIndex));
                }

                writer.Dispose();
                stream.Dispose();
                MelonLogger.Msg("Done, saved to " + MelonUtils.BaseDirectory + "/" + "TypeInfo.bin");
            }
        }

        private void WriteType(BinaryWriter writer, Type t) {
            if (t == null) {
                writer.Write(0); // assembly name
                writer.Write(0); // type name
                writer.Write(0); // generics count
                return;
            }
            
            var asmName = t.Assembly.GetName().Name;
            writer.Write(asmName.Length);
            writer.Write(Encoding.UTF8.GetBytes(asmName));

            var name = t.FullName;
            if (t.IsConstructedGenericType) {
                name = t.GetGenericTypeDefinition().FullName;
            }
            writer.Write(name.Length);
            writer.Write(Encoding.UTF8.GetBytes(name));

            if (t.IsConstructedGenericType) {
                var typeArgs = t.GetGenericArguments();
                writer.Write(typeArgs.Length);
                foreach (var typeArg in typeArgs) {
                    WriteType(writer, typeArg);
                }
            } else {
                writer.Write(0);
            }
        }
    }
}
