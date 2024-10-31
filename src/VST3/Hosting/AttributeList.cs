using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3.Hosting;

[GeneratedComClass]
sealed partial class AttributeList : IAttributeList
{
    record BinaryBlob(nint Data, uint SizeInBytes);

    private readonly Dictionary<string, object> data = new();

    unsafe void IAttributeList.getBinary(string id, out nint data, out uint sizeInBytes)
    {
        if (this.data.TryGetValue(id, out var value) && value is BinaryBlob blob)
        {
            data = blob.Data;
            sizeInBytes = blob.SizeInBytes;
        }
        else
        {
            data = default;
            sizeInBytes = default;
        }
    }

    double IAttributeList.getFloat(string id)
    {
        if (data.TryGetValue(id, out var value) && value is double d)
            return d;
        return default;
    }

    long IAttributeList.getInt(string id)
    {
        if (data.TryGetValue(id, out var value) && value is long l)
            return l;
        return default;
    }

    void IAttributeList.getString(string id, [MarshalUsing(typeof(Utf16StringMarshaller))] ref string value, uint sizeInBytes)
    {
        if (data.TryGetValue(id, out var v) && v is string s)
        {
            value = s;
        }
    }

    void IAttributeList.setBinary(string id, nint data, uint sizeInBytes)
    {
        this.data[id] = new BinaryBlob(data, sizeInBytes);
    }

    void IAttributeList.setFloat(string id, double value)
    {
        data[id] = value;
    }

    void IAttributeList.setInt(string id, long value)
    {
        data[id] = value;
    }

    void IAttributeList.setString(string id, [MarshalAs(UnmanagedType.LPWStr)] string value)
    {
        data[id] = value;
    }
}
