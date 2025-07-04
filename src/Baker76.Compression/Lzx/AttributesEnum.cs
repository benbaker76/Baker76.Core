using System;

namespace Baker76.Compression.Lzx
{
    [Flags]
    public enum AttributesEnum
    {
        HeldResident = 32,
        Script = 64,
        Pure = 128,
        Archive = 16,
        Read = 1,
        Write = 2,
        Executable = 8,
        Delete = 4
    }    
}
