using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Stardust.Utilities
{
    internal static class EndiannessCheck
    {
        /// <summary>
        /// Ensures the assembly is loaded on an architecture matching its compilation target.
        /// </summary>
        [ModuleInitializer]
        [SuppressMessage("Performance", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
            Justification = "Required for platform compatibility check - must fail fast before any library code executes.")]
        internal static void EnsureCorrectEndianness()
        {
#if BIG_ENDIAN
            if (BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException("The Stardust.Utilities library was compiled for Big-Endian architectures but is running on a Little-Endian architecture.");
            }
#else
            if (!BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException("The Stardust.Utilities library was compiled for Little-Endian architectures but is running on a Big-Endian architecture.");
            }
#endif
        }
    }
}
