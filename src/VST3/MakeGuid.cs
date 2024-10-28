var guid = MakeGuid(0x65ED9690, 0x8AC44525, 0x8AADEF7A, 0x72EA703F);

Console.WriteLine(guid);

static Guid MakeGuid(uint l1, uint l2, uint l3, uint l4)
{
    return new Guid([
        (byte)((l1 & 0x000000FF)      ), (byte)((l1 & 0x0000FF00) >>  8),
        (byte)((l1 & 0x00FF0000) >> 16), (byte)((l1 & 0xFF000000) >> 24),
        (byte)((l2 & 0x00FF0000) >> 16), (byte)((l2 & 0xFF000000) >> 24),
        (byte)((l2 & 0x000000FF)      ), (byte)((l2 & 0x0000FF00) >>  8),
        (byte)((l3 & 0xFF000000) >> 24), (byte)((l3 & 0x00FF0000) >> 16),
        (byte)((l3 & 0x0000FF00) >>  8), (byte)((l3 & 0x000000FF)      ),
        (byte)((l4 & 0xFF000000) >> 24), (byte)((l4 & 0x00FF0000) >> 16),
        (byte)((l4 & 0x0000FF00) >>  8), (byte)((l4 & 0x000000FF)      ),
        ]);
}