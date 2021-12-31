using System;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects;
using Snuggle.Core.Models.Objects.Audio;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.AudioClip)]
public class AudioClip : NamedObject {
    public AudioClip(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        LoadType = (AudioLoadType) reader.ReadInt32();
        Channels = reader.ReadInt32();
        Frequency = reader.ReadInt32();
        SampleRate = reader.ReadInt32();
        Duration = reader.ReadSingle();
        IsTrackerFormat = reader.ReadBoolean();
        if (SerializedFile.Version >= UnityVersionRegister.Unity2020_1) {
            Ambisonic = reader.ReadBoolean();
        }

        reader.Align();

        SubsoundIndex = reader.ReadInt32();

        PreloadAudioData = reader.ReadBoolean();
        LoadInBackground = reader.ReadBoolean();
        Legacy3D = reader.ReadBoolean();

        Resource = StreamingInfo.FromReader(reader, serializedFile);
        Format = (AudioCompressionFormat) reader.ReadInt32();
    }

    public AudioClip(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) => Resource = StreamingInfo.Null;

    public AudioLoadType LoadType { get; set; }
    public int Channels { get; set; }
    public int Frequency { get; set; }
    public int SampleRate { get; set; }
    public float Duration { get; set; }
    public bool IsTrackerFormat { get; set; }
    public bool Ambisonic { get; set; }
    public int SubsoundIndex { get; set; }
    public bool PreloadAudioData { get; set; }
    public bool LoadInBackground { get; set; }
    public bool Legacy3D { get; set; }
    public StreamingInfo Resource { get; set; }
    public AudioCompressionFormat Format { get; set; }
    public Memory<byte>? Data { get; set; }

    private bool ShouldDeserializeData => Data == null;
    public override bool ShouldDeserialize => base.ShouldDeserialize || ShouldDeserializeData;

    public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        Data = Resource.GetData(SerializedFile.Assets, options);
    }
}
