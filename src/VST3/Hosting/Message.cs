using System.Runtime.InteropServices.Marshalling;

namespace VST3.Hosting;

[GeneratedComClass]
sealed partial class Message : IMessage
{
    private string? id;
    private AttributeList? attributeList;

    IAttributeList IMessage.getAttributes()
    {
        return attributeList ??= new AttributeList();
    }

    string IMessage.getMessageID()
    {
        return id ?? string.Empty;
    }

    void IMessage.setMessageID(string id)
    {
        this.id = id;
    }
}
