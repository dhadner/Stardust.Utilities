using Stardust.Utilities;

namespace BitFields.DemoApp;

/// <summary>
/// DOS Header (IMAGE_DOS_HEADER) - 64 bytes at offset 0.
/// </summary>
[BitFieldsView(ByteOrder.LittleEndian, BitOrder.BitZeroIsLsb)]
public partial record struct DosHeaderView
{
    [BitField(0, 15, Description = "Magic number identifying a DOS executable (0x5A4D = 'MZ')")]
    public partial ushort Magic { get; set; }

    [BitField(16, 31, Description = "Bytes on last page of the file")]
    public partial ushort Cblp { get; set; }

    [BitField(32, 47, Description = "Number of pages in the file")]
    public partial ushort Cp { get; set; }

    [BitField(48, 63, Description = "Number of relocations")]
    public partial ushort Crlc { get; set; }

    [BitField(64, 79, Description = "Size of the header in paragraphs (16-byte units)")]
    public partial ushort Cparhdr { get; set; }

    [BitField(80, 95, Description = "Minimum extra paragraphs needed")]
    public partial ushort Minalloc { get; set; }

    [BitField(96, 111, Description = "Maximum extra paragraphs needed")]
    public partial ushort Maxalloc { get; set; }

    [BitField(112, 127, Description = "Initial relative SS (stack segment) value")]
    public partial ushort Ss { get; set; }

    [BitField(128, 143, Description = "Initial SP (stack pointer) value")]
    public partial ushort Sp { get; set; }

    [BitField(144, 159, Description = "Checksum of the DOS header")]
    public partial ushort Csum { get; set; }

    [BitField(160, 175, Description = "Initial IP (instruction pointer) value")]
    public partial ushort Ip { get; set; }

    [BitField(176, 191, Description = "Initial relative CS (code segment) value")]
    public partial ushort Cs { get; set; }

    [BitField(192, 207, Description = "File offset of the relocation table")]
    public partial ushort Lfarlc { get; set; }

    [BitField(208, 223, Description = "Overlay number")]
    public partial ushort Ovno { get; set; }

    // e_res[4] = 8 bytes at offset 28 (bits 224-287) - reserved
    // e_oemid, e_oeminfo, e_res2[10] = 24 bytes at offset 36 (bits 288-479) - reserved

    [BitField(480, 511, Description = "File offset of the PE header (points to 'PE\\0\\0' signature)")]
    public partial uint Lfanew { get; set; }
}

public static class PeHeader
{
    public const uint Signature = 0x00004550;
}

/// <summary>
/// COFF File Header (IMAGE_FILE_HEADER) - 20 bytes following the PE signature.
/// </summary>
[BitFieldsView(ByteOrder.LittleEndian, BitOrder.BitZeroIsLsb)]
public partial record struct CoffHeaderView
{
    [BitField(0, 15, Description = "Target CPU architecture (0x8664=AMD64, 0x14C=i386, 0xAA64=ARM64)")]
    public partial ushort Machine { get; set; }

    [BitField(16, 31, Description = "Number of sections in the PE file")]
    public partial ushort NumberOfSections { get; set; }

    [BitField(32, 63, Description = "Unix timestamp when the file was created by the linker")]
    public partial uint TimeDateStamp { get; set; }

    [BitField(64, 95, Description = "File offset of the COFF symbol table (0 if none)")]
    public partial uint PointerToSymbolTable { get; set; }

    [BitField(96, 127, Description = "Number of entries in the symbol table")]
    public partial uint NumberOfSymbols { get; set; }

    [BitField(128, 143, Description = "Size of the Optional Header in bytes")]
    public partial ushort SizeOfOptionalHeader { get; set; }

    [BitField(144, 159, Description = "Bitmask of file attributes (DLL, executable, large address aware, etc.)")]
    public partial ushort Characteristics { get; set; }
}

/// <summary>
/// Optional Header standard + Windows-specific fields (IMAGE_OPTIONAL_HEADER64 / PE32+).
/// </summary>
[BitFieldsView(ByteOrder.LittleEndian, BitOrder.BitZeroIsLsb)]
public partial record struct OptionalHeaderView
{
    [BitField(0, 15, Description = "Optional header magic (0x10B=PE32, 0x20B=PE32+)")]
    public partial ushort OptMagic { get; set; }

    [BitField(16, 23, Description = "Major version of the linker that produced this file")]
    public partial byte MajorLinkerVersion { get; set; }

    [BitField(24, 31, Description = "Minor version of the linker that produced this file")]
    public partial byte MinorLinkerVersion { get; set; }

    [BitField(32, 63, Description = "Total size of all code (text) sections")]
    public partial uint SizeOfCode { get; set; }

    [BitField(64, 95, Description = "Total size of all initialized data sections")]
    public partial uint SizeOfInitializedData { get; set; }

    [BitField(96, 127, Description = "Total size of all uninitialized data (BSS) sections")]
    public partial uint SizeOfUninitializedData { get; set; }

    [BitField(128, 159, Description = "RVA of the entry point function (e.g., main or DllMain)")]
    public partial uint AddressOfEntryPoint { get; set; }

    [BitField(160, 191, Description = "RVA of the beginning of the code section")]
    public partial uint BaseOfCode { get; set; }

    [BitField(192, 255, Description = "Preferred virtual address of the first byte of the image when loaded")]
    public partial ulong ImageBase { get; set; }

    [BitField(256, 287, Description = "Alignment of sections in memory (must be >= FileAlignment)")]
    public partial uint SectionAlignment { get; set; }

    [BitField(288, 319, Description = "Alignment of raw section data in the file (typically 512 or 4096)")]
    public partial uint FileAlignment { get; set; }

    [BitField(320, 335, Description = "Minimum required OS major version")]
    public partial ushort MajorOSVersion { get; set; }

    [BitField(336, 351, Description = "Minimum required OS minor version")]
    public partial ushort MinorOSVersion { get; set; }

    [BitField(352, 367, Description = "Major version number of the image")]
    public partial ushort MajorImageVersion { get; set; }

    [BitField(368, 383, Description = "Minor version number of the image")]
    public partial ushort MinorImageVersion { get; set; }

    [BitField(384, 399, Description = "Major version of the required subsystem")]
    public partial ushort MajorSubsystemVersion { get; set; }

    [BitField(400, 415, Description = "Minor version of the required subsystem")]
    public partial ushort MinorSubsystemVersion { get; set; }

    [BitField(416, 447, Description = "Reserved, must be zero")]
    public partial uint Win32VersionValue { get; set; }

    [BitField(448, 479, Description = "Total size of the image in memory, rounded up to SectionAlignment")]
    public partial uint SizeOfImage { get; set; }

    [BitField(480, 511, Description = "Combined size of DOS header, PE headers, and section headers, rounded to FileAlignment")]
    public partial uint SizeOfHeaders { get; set; }

    [BitField(512, 543, Description = "Image file checksum (validated for drivers and critical system DLLs)")]
    public partial uint CheckSum { get; set; }

    [BitField(544, 559, Description = "Required Windows subsystem (2=GUI, 3=Console, 10=EFI)")]
    public partial ushort Subsystem { get; set; }

    [BitField(560, 575, Description = "DLL characteristics flags (ASLR, DEP/NX, high-entropy ASLR, etc.)")]
    public partial ushort DllCharacteristics { get; set; }
}
