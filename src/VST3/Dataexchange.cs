using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace VST3;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("36D551BD-6FF5-4F08-B48E-830D8BD5A03B")]
unsafe partial interface IDataExchangeHandler
{
    /// <summary>
    /// Open a new queue.
    /// Only allowed to be called from the main thread when the component is not active but initialized and connected (see IConnectionPoint).
    /// </summary>
    /// <param name="processor">The processor who wants to open the queue.</param>
    /// <param name="blockSize">Size of one block.</param>
    /// <param name="numBlocks">Number of blocks in the queue.</param>
    /// <param name="alignment">Data alignment, if zero will use the platform default alignment if any.</param>
    /// <param name="userContextID">An identifier internal to the processor.</param>
    /// <param name="outID">On return the ID of the queue.</param>
    /// <returns>kResultTrue on success.</returns>
    [PreserveSig]
    [return: MarshalUsing(typeof(VstBoolMarshaller))]
    bool openQueue(IntPtr processor, uint blockSize, uint numBlocks, uint alignment, uint userContextID, out uint outID);

    /// <summary>
    /// Close a queue.
    /// Closes and frees all memory of a previously opened queue.
    /// Only allowed to be called from the main thread when the component is not active but initialized and connected.
    /// </summary>
    /// <param name="queueID">The ID of the queue to close.</param>
    /// <returns>kResultTrue on success.</returns>
    [PreserveSig] 
    [return: MarshalUsing(typeof(VstBoolMarshaller))]
    bool closeQueue(uint queueID);

    /// <summary>
    /// Lock a block if available.
    /// Only allowed to be called from within the IAudioProcessor::process call.
    /// </summary>
    /// <param name="queueID">The ID of the queue.</param>
    /// <param name="block">On return will contain the data pointer and size of the block.</param>
    /// <returns>kResultTrue if a free block was found and kOutOfMemory if all blocks are locked.</returns>
    [PreserveSig]
    [return: MarshalUsing(typeof(VstBoolMarshaller))]
    bool lockBlock(uint queueID, ref DataExchangeBlock block);

    /// <summary>
    /// Free a previously locked block.
    /// Only allowed to be called from within the IAudioProcessor::process call.
    /// </summary>
    /// <param name="queueID">The ID of the queue.</param>
    /// <param name="blockID">The ID of the block.</param>
    /// <param name="sendToController">If true the block data will be sent to the IEditController otherwise it will be discarded.</param>
    /// <returns>kResultTrue on success.</returns>
    [PreserveSig]
    [return: MarshalUsing(typeof(VstBoolMarshaller))]
    bool freeBlock(uint queueID, uint blockID, [MarshalAs(UnmanagedType.U1)] bool sendToController);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("45A759DC-84FA-4907-ABCB-61752FC786B6")]
unsafe partial interface IDataExchangeReceiver
{
    /// <summary>
    /// Queue opened notification.
    /// Called on the main thread when the processor has opened a queue.
    /// </summary>
    /// <param name="userContextID">The user context ID of the queue.</param>
    /// <param name="blockSize">The size of one block of the queue.</param>
    /// <param name="dispatchOnBackgroundThread">If true on output the blocks are dispatched on a background thread.</param>
    void queueOpened(uint userContextID, uint blockSize, [MarshalAs(UnmanagedType.U1)] ref bool dispatchOnBackgroundThread);

    /// <summary>
    /// Queue closed notification.
    /// Called on the main thread when the processor has closed a queue.
    /// </summary>
    /// <param name="userContextID">The user context ID of the queue.</param>
    void queueClosed(uint userContextID);

    /// <summary>
    /// One or more blocks were received.
    /// Called either on the main thread or a background thread depending on the dispatchOnBackgroundThread value in the queueOpened call.
    /// </summary>
    /// <param name="userContextID">The user context ID of the queue.</param>
    /// <param name="numBlocks">Number of blocks.</param>
    /// <param name="blocks">The blocks.</param>
    /// <param name="onBackgroundThread">True if the call is done on a background thread.</param>
    void onDataExchangeBlocksReceived(uint userContextID, uint numBlocks, ReadOnlySpan<DataExchangeBlock> blocks, [MarshalAs(UnmanagedType.U1)] bool onBackgroundThread);
}

[StructLayout(LayoutKind.Sequential)]
public struct DataExchangeBlock
{
    public IntPtr data;
    public uint size;
    public uint blockID;
}

static partial class Constants
{
    public const uint InvalidDataExchangeQueueID = uint.MaxValue;
    public const uint InvalidDataExchangeBlockID = uint.MaxValue;
}