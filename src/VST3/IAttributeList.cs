using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3;

//------------------------------------------------------------------------
/** Attribute list used in IMessage and IStreamAttributes: Vst::IAttributeList
\ingroup vstIHost vst300
- [host imp]
- [released: 3.0.0]
- [mandatory]

An attribute list associates values with a key (id: some predefined keys can be found in \ref
presetAttributes).
*/
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
[Guid("1E5F0AEB-CC7F-4533-A254-401138AD5EE4")]
partial interface IAttributeList
{
	/** Sets integer value. */
	void setInt(string id, long value);

	/** Gets integer value. */
	long getInt(string id);

	/** Sets float value. */
	void setFloat(string id, double value);

	/** Gets float value. */
	double getFloat(string id);

	/** Sets string value (UTF16) (must be null-terminated!). */
	void setString(string id, [MarshalAs(UnmanagedType.LPWStr)] string value);

	/** Gets string value (UTF16). Note that Size is in Byte, not the string Length!
		Do not forget to multiply the length by sizeof (TChar)! */
	void getString(string id, [MarshalUsing(typeof(Utf16StringMarshaller))] ref string value, uint sizeInBytes);

	/** Sets binary data. */
	void setBinary(string id, nint data, uint sizeInBytes);

	/** Gets binary data. */
	void getBinary(string id, out nint data, out uint sizeInBytes);
}