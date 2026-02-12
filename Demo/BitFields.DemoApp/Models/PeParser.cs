using Stardust.Utilities;

namespace BitFields.DemoApp;

/// <summary>
/// Parsed PE header data produced by <see cref="PeParser"/>.
/// Contains the validated headers and pre-computed byte offsets needed for field display.
/// </summary>
public sealed class PeParseResult
{
    public required byte[] Bytes { get; init; }
    public required DosHeaderView Dos { get; init; }
    public required uint Signature { get; init; }
    public required CoffHeaderView Coff { get; init; }
    public required int PeOffset { get; init; }
    public required int CoffByteOffset { get; init; }
    public required int TotalDisplayBytes { get; init; }
    public OptionalHeaderView? Optional { get; init; }
    public int OptByteOffset { get; init; }
}

/// <summary>
/// Demonstrates <see cref="Result{T, TError}"/> with <c>.Then()</c> chaining
/// to validate and parse PE file headers in a pipeline style.
/// Each step either succeeds with progressively richer data
/// or short-circuits with a descriptive error string.
/// </summary>
public static class PeParser
{
    /// <summary>
    /// Parses PE headers from raw bytes using a Result pipeline.
    /// </summary>
    /// <returns>
    /// <c>Result&lt;PeParseResult, string&gt;</c> -- either a fully validated
    /// parse result or an error message describing what went wrong.
    /// </returns>
    public static Result<PeParseResult, string> Parse(byte[] bytes)
    {
        return ValidateDosHeader(bytes)
            .Then(dos => ValidatePeSignature(bytes, dos))
            .Then(context => ParseCoffHeader(bytes, context))
            .Then(context => BuildResult(bytes, context));
    }

    // ── Pipeline stages ──────────────────────────────────────

    private static Result<DosHeaderView, string> ValidateDosHeader(byte[] bytes)
    {
        if (bytes.Length < DosHeaderView.SizeInBytes)
            return Result<DosHeaderView, string>.Err("File too small for a DOS header.");

        return Result<DosHeaderView, string>.Ok(new DosHeaderView(bytes));
    }

    private static Result<(DosHeaderView Dos, int PeOffset, uint Signature), string>
        ValidatePeSignature(byte[] bytes, DosHeaderView dos)
    {
        int peOffset = (int)dos.Lfanew;

        if (peOffset < 0 || peOffset + 4 > bytes.Length)
            return Result<(DosHeaderView, int, uint), string>.Err(
                $"Invalid PE offset (0x{peOffset:X}) -- points beyond end of file.");

        uint sig = BitConverter.ToUInt32(bytes, peOffset);
        if (sig != PeHeader.Signature)
            return Result<(DosHeaderView, int, uint), string>.Err(
                $"Bad PE signature: expected 0x{PeHeader.Signature:X8}, got 0x{sig:X8}.");

        return Result<(DosHeaderView, int, uint), string>.Ok((dos, peOffset, sig));
    }

    private static Result<(DosHeaderView Dos, int PeOffset, uint Signature, CoffHeaderView Coff, int CoffByteOffset), string>
        ParseCoffHeader(byte[] bytes, (DosHeaderView Dos, int PeOffset, uint Signature) ctx)
    {
        int coffByteOffset = ctx.PeOffset + 4;

        if (bytes.Length < coffByteOffset + CoffHeaderView.SizeInBytes)
            return Result<(DosHeaderView, int, uint, CoffHeaderView, int), string>.Err(
                "File truncated -- COFF header extends beyond end of file.");

        var coff = new CoffHeaderView(bytes, coffByteOffset);
        return Result<(DosHeaderView, int, uint, CoffHeaderView, int), string>.Ok(
            (ctx.Dos, ctx.PeOffset, ctx.Signature, coff, coffByteOffset));
    }

    private static Result<PeParseResult, string> BuildResult(
        byte[] bytes,
        (DosHeaderView Dos, int PeOffset, uint Signature, CoffHeaderView Coff, int CoffByteOffset) ctx)
    {
        int optByteOffset = ctx.CoffByteOffset + CoffHeaderView.SizeInBytes;
        int optHeaderSize = Math.Min((int)ctx.Coff.SizeOfOptionalHeader, OptionalHeaderView.SizeInBytes);
        bool hasOptional = ctx.Coff.SizeOfOptionalHeader > 0
                           && bytes.Length >= optByteOffset + optHeaderSize;

        int totalDisplayBytes = hasOptional
            ? Math.Min(optByteOffset + optHeaderSize, bytes.Length)
            : Math.Min(ctx.CoffByteOffset + CoffHeaderView.SizeInBytes, bytes.Length);

        var result = new PeParseResult
        {
            Bytes = bytes,
            Dos = ctx.Dos,
            Signature = ctx.Signature,
            Coff = ctx.Coff,
            PeOffset = ctx.PeOffset,
            CoffByteOffset = ctx.CoffByteOffset,
            TotalDisplayBytes = totalDisplayBytes,
            OptByteOffset = optByteOffset,
            Optional = hasOptional ? new OptionalHeaderView(bytes, optByteOffset) : null
        };

        return Result<PeParseResult, string>.Ok(result);
    }
}
