using System.Xml.Serialization;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PremiumUI.Tests;

/// <summary>
/// IXmlSerializer implementation backed by System.Xml.Serialization, mirroring Jellyfin's serializer.
/// </summary>
internal sealed class TestXmlSerializer : IXmlSerializer
{
    public object DeserializeFromStream(Type type, Stream stream)
        => new XmlSerializer(type).Deserialize(stream)!;

    public object DeserializeFromFile(Type type, string file)
    {
        using var stream = File.OpenRead(file);
        return DeserializeFromStream(type, stream);
    }

    public object DeserializeFromBytes(Type type, byte[] buffer)
    {
        using var stream = new MemoryStream(buffer);
        return DeserializeFromStream(type, stream);
    }

    public void SerializeToStream(object obj, Stream stream)
        => new XmlSerializer(obj.GetType()).Serialize(stream, obj);

    public void SerializeToFile(object obj, string file)
    {
        using var stream = File.Create(file);
        SerializeToStream(obj, stream);
    }
}
