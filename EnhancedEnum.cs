using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Enhanced enum record. Allows subclasses to store a value associated with an enum kind
    /// similar to a discriminated union or a Rust enum.
    /// </summary>
    /// <typeparam name="TEnum">The enum type representing variant kinds.</typeparam>
    public abstract record EnhancedEnum<TEnum> where TEnum : Enum
    {
        // Mapping from TKind to the value's Type associated with that kind.
        private static readonly Dictionary<TEnum, Type?> _kindTypes = [];

        /// <summary>
        /// Enum case.
        /// </summary>
        public TEnum Kind { get; init; }

        /// <summary>
        /// Value associated with the enum case.
        /// </summary>
        public object? Value { get; init; }

        /// <summary>
        /// Standard constructor storing kind and associated value.
        /// </summary>
        /// <param name="kind">The variant kind.</param>
        /// <param name="value">The associated value.</param>
        protected EnhancedEnum(TEnum kind, object? value)
        {
            Kind = kind;
            Value = value;
            CheckOrRegisterKindType(kind, value);
        }

        /// <summary>
        /// True if flag is set.
        /// </summary>
        /// <param name="flag"></param>
        /// <returns>True if flag is set, else false</returns>
        public bool HasFlag(TEnum flag)
        {
            var kindValue = Convert.ToInt64(Kind);
            var flagValue = Convert.ToInt64(flag);
            return (kindValue & flagValue) == flagValue;
        }

        /// <summary>
        /// Enables implicit comparison with TEnum values without accessing .Kind.
        /// Usage: if (myEnum == MyEnumKind.Int) { ... }
        /// </summary>
        /// <param name="kind">The enum kind to compare against.</param>
        /// <returns>True if this instance's Kind equals the specified kind.</returns>
        public bool Is(TEnum kind) => EqualityComparer<TEnum>.Default.Equals(Kind, kind);

        /// <summary>
        /// Pattern-match style accessor: checks kind and extracts typed value in one call.
        /// Usage: if (myEnum.Is(MyEnumKind.Int, out int value)) { ... }
        /// </summary>
        /// <typeparam name="T">Expected value type.</typeparam>
        /// <param name="kind">The enum kind to match.</param>
        /// <param name="value">The extracted value if matched.</param>
        /// <returns>True if kind matches and value is of type T.</returns>
        public bool Is<T>(TEnum kind, out T value)
        {
            if (Is(kind) && TryValueAs(out value!))
            {
                return true;
            }
            value = default!;
            return false;
        }

        /// <summary>
        /// Enables tuple deconstruction and positional pattern matching.
        /// </summary>
        /// <param name="kind">Out: the variant kind.</param>
        /// <param name="value">Out: the associated boxed value.</param>
        public void Deconstruct(out TEnum kind, out object? value)
        {
            kind = Kind;
            value = Value;
        }

        /// <summary>
        /// Ensure the value is of the registered type or register new type if not registered yet.
        /// </summary>
        /// <param name="kind">The enum kind.</param>
        /// <param name="value">The value to check/register.</param>
        /// <exception cref="ArgumentException">If value type doesn't match registered type.</exception>
        protected static void CheckOrRegisterKindType(TEnum kind, object? value)
        {
            if (!_kindTypes.TryGetValue(kind, out Type? valueType))
            {
                RegisterKindType(kind, value?.GetType());
            }
            else
            {
                if (valueType != null && valueType != value?.GetType())
                {
                    throw new ArgumentException($"Incompatible value type for kind {kind}: expected {valueType}, got {value?.GetType()}");
                }
            }
        }

        /// <summary>
        /// Register the value type for the given kind.
        /// </summary>
        /// <param name="kind">The enum kind.</param>
        /// <param name="valueType">The CLR type for values of this kind.</param>
        protected static void RegisterKindType(TEnum kind, Type? valueType)
        {
            _kindTypes[kind] = valueType;
        }

        /// <summary>
        /// Generic typed accessor. Attempts to cast the stored value to T.
        /// </summary>
        /// <typeparam name="T">Requested type.</typeparam>
        /// <returns>Value cast to T.</returns>
        /// <exception cref="InvalidCastException">If the stored value cannot be cast to T.</exception>
        public T ValueAs<T>()
        {
            if (TryValueAs<T>(out T? v))
            {
                return v!;
            }
            throw new InvalidCastException($"Stored value is not a {typeof(T).FullName}");
        }

        /// <summary>
        /// Try to get the stored value as the requested type.
        /// </summary>
        /// <typeparam name="T">Requested type.</typeparam>
        /// <param name="value">Out value if cast succeeds.</param>
        /// <returns>True if cast succeeded.</returns>
        public bool TryValueAs<T>(out T? value)
        {
            var current = Value;
            var targetType = typeof(T);

            if (current is null)
            {
                if (Nullable.GetUnderlyingType(targetType) != null)
                {
                    value = default;
                    return true;
                }
                value = default;
                return false;
            }

            if (current is T matched)
            {
                value = matched;
                return true;
            }

            var nullableUnderlying = Nullable.GetUnderlyingType(targetType);
            var effectiveTarget = nullableUnderlying ?? targetType;

            if (effectiveTarget.IsInstanceOfType(current))
            {
                value = (T)current;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Allows implicit conversion to TEnum for direct comparison.
        /// Usage: TEnum k = myEnhancedEnum;
        /// </summary>
        /// <param name="e">The enhanced enum instance.</param>
        public static implicit operator TEnum(EnhancedEnum<TEnum> e) => e.Kind;
    }

    /// <summary>
    /// Union struct for inline value-type payloads used by <see cref="EnhancedEnumFlex{TEnum}"/>.
    /// All fields share the same memory location to minimize struct size.
    /// Only 8 bytes total regardless of which payload type is used.
    /// Must be defined outside the generic type due to CLR restrictions on explicit layout.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct EnhancedEnumFlexInlinePayload
    {
        [FieldOffset(0)] public byte Byte;       // PayloadKind.Byte
        [FieldOffset(0)] public ushort UInt16;   // PayloadKind.UInt16
        [FieldOffset(0)] public uint UInt32;     // PayloadKind.UInt32
        [FieldOffset(0)] public uint UInt32_A;   // First element of tuple payloads
        [FieldOffset(4)] public uint UInt32_B;   // Second element (uint variant)
        [FieldOffset(4)] public int Int32_B;     // Second element (int variant)
    }

    /// <summary>
    /// Hot-path friendly discriminated union that avoids boxing for common value payloads
    /// and supports reference payloads (including <see cref="string"/>) and generic boxed
    /// payloads as well.
    ///
    /// This is intended to complement (not replace) <see cref="EnhancedEnum{TEnum}"/>.
    /// Use <see cref="EnhancedEnum{TEnum}"/> where flexibility and pattern matching are most important.
    /// Use this struct where allocation/boxing must be minimized.
    /// </summary>
    /// <typeparam name="TEnum">Enum tag type representing the variant kind.</typeparam>
    public readonly record struct EnhancedEnumFlex<TEnum> where TEnum : struct, Enum
    {
        /// <summary>
        /// Discriminator for the type of payload stored in this instance.
        /// </summary>
        public enum PayloadKind : byte
        {
            None = 0,        // No payload
            Byte = 1,        // byte stored in _inline.Byte
            UInt16 = 2,      // ushort stored in _inline.UInt16
            UInt32 = 3,      // uint stored in _inline.UInt32
            UInt32_UInt32 = 4, // (uint,uint) stored in _inline.UInt32_A/B
            UInt32_Int32 = 5,  // (uint,int) stored in _inline.UInt32_A/Int32_B
            String = 253,    // string stored in _ref
            Reference = 254, // general reference stored in _ref
            BoxedValue = 255, // boxed value type stored in _ref
        }

        /// <summary>
        /// Variant kind.
        /// </summary>
        public TEnum Kind { get; }

        private readonly PayloadKind _payloadKind;

        // Inline value payload union (8 bytes - all value payloads share this memory).
        private readonly EnhancedEnumFlexInlinePayload _inline;

        // Single reference slot for string, general reference, or boxed value payloads.
        // CLR restriction: reference types cannot overlap with value types.
        private readonly object? _ref;

        private static readonly Dictionary<TEnum, PayloadKind> _kindPayloadKinds = [];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EnhancedEnumFlex(TEnum kind, PayloadKind payloadKind)
        {
            Kind = kind;
            _payloadKind = payloadKind;
            _inline = default;
            _ref = null;
        }

        /// <summary>
        /// Register the expected payload kind for the specified enum case.
        /// Each enum case must be associated with one and only one payload kind.
        /// </summary>
        /// <param name="kind">The enum case to record.</param>
        /// <param name="payloadKind">The payload kind associated with this enum case.</param>
        public static void RegisterKindPayloadKind(TEnum kind, PayloadKind payloadKind)
        {
            if (_kindPayloadKinds.TryGetValue(kind, out var existing) && existing != payloadKind)
            {
                string message = $"Incompatible payload kind registration for kind {kind}: existing {existing}, new {payloadKind}.";
#if DEBUG
                Debug.Assert(false, message); // Non-fatal assertion
#endif
                throw new ArgumentException(message);
            }
            _kindPayloadKinds[kind] = payloadKind;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertPayloadKind(TEnum kind, PayloadKind expected)
        {
            if (!_kindPayloadKinds.TryGetValue(kind, out var actual))
            {
                string message = $"No payload kind has been registered for kind {kind}.";
#if DEBUG
                Debug.Assert(false, message); // Non-fatal assertion
#endif
                throw new ArgumentException(message);
            }

            if (actual != expected)
            {
                string message = $"Incompatible payload kind for kind {kind}: expected {actual}, attempted {expected}.";
#if DEBUG
                Debug.Assert(false, message); // Non-fatal assertion
#endif
                throw new ArgumentException(message);
            }
        }

        /// <summary>
        /// Creates a tag-only value (no payload).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnhancedEnumFlex<TEnum> Create(TEnum kind) => new(kind, PayloadKind.None);

        /// <summary>
        /// Creates a value with <see cref="byte"/> payload (no boxing).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnhancedEnumFlex<TEnum> Create(TEnum kind, byte value)
        {
            AssertPayloadKind(kind, PayloadKind.Byte);
            var e = new EnhancedEnumFlex<TEnum>(kind, PayloadKind.Byte);
            Unsafe.AsRef(in e._inline).Byte = value;
            return e;
        }

        /// <summary>
        /// Creates a value with <see cref="ushort"/> payload (no boxing).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnhancedEnumFlex<TEnum> Create(TEnum kind, ushort value)
        {
            AssertPayloadKind(kind, PayloadKind.UInt16);
            var e = new EnhancedEnumFlex<TEnum>(kind, PayloadKind.UInt16);
            Unsafe.AsRef(in e._inline).UInt16 = value;
            return e;
        }

        /// <summary>
        /// Creates a value with <see cref="uint"/> payload (no boxing).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnhancedEnumFlex<TEnum> Create(TEnum kind, uint value)
        {
            AssertPayloadKind(kind, PayloadKind.UInt32);
            var e = new EnhancedEnumFlex<TEnum>(kind, PayloadKind.UInt32);
            Unsafe.AsRef(in e._inline).UInt32 = value;
            return e;
        }

        /// <summary>
        /// Creates a value with <c>(uint,uint)</c> payload (no boxing).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnhancedEnumFlex<TEnum> Create(TEnum kind, (uint a, uint b) value)
        {
            AssertPayloadKind(kind, PayloadKind.UInt32_UInt32);
            var e = new EnhancedEnumFlex<TEnum>(kind, PayloadKind.UInt32_UInt32);
            Unsafe.AsRef(in e._inline).UInt32_A = value.a;
            Unsafe.AsRef(in e._inline).UInt32_B = value.b;
            return e;
        }

        /// <summary>
        /// Creates a value with <c>(uint,int)</c> payload (no boxing).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnhancedEnumFlex<TEnum> Create(TEnum kind, (uint a, int b) value)
        {
            AssertPayloadKind(kind, PayloadKind.UInt32_Int32);
            var e = new EnhancedEnumFlex<TEnum>(kind, PayloadKind.UInt32_Int32);
            Unsafe.AsRef(in e._inline).UInt32_A = value.a;
            Unsafe.AsRef(in e._inline).Int32_B = value.b;
            return e;
        }

        /// <summary>
        /// Creates a value with <see cref="string"/> payload (stored directly; no extra allocation).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnhancedEnumFlex<TEnum> Create(TEnum kind, string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            AssertPayloadKind(kind, PayloadKind.String);
            var e = new EnhancedEnumFlex<TEnum>(kind, PayloadKind.String);
            Unsafe.AsRef(in e._ref) = value;
            return e;
        }

        /// <summary>
        /// Creates a value with a general reference payload (any class). No extra allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnhancedEnumFlex<TEnum> CreateReference<T>(TEnum kind, T value) where T : class
        {
            ArgumentNullException.ThrowIfNull(value);
            AssertPayloadKind(kind, PayloadKind.Reference);
            var e = new EnhancedEnumFlex<TEnum>(kind, PayloadKind.Reference);
            Unsafe.AsRef(in e._ref) = value;
            return e;
        }

        /// <summary>
        /// Creates a value with a struct payload. Routes hot-path types to inline storage,
        /// falls back to boxing for other value types.
        /// </summary>
        /// <typeparam name="T">The value type to store.</typeparam>
        /// <param name="kind">The enum kind.</param>
        /// <param name="value">The value to store.</param>
        /// <returns>A new EnhancedEnumFlex instance.</returns>
        public static EnhancedEnumFlex<TEnum> CreateValue<T>(TEnum kind, T value) where T : struct
        {
            // Route hot-path types with no boxing
            if (typeof(T) == typeof(byte)) return Create(kind, Unsafe.As<T, byte>(ref value));
            if (typeof(T) == typeof(ushort)) return Create(kind, Unsafe.As<T, ushort>(ref value));
            if (typeof(T) == typeof(uint)) return Create(kind, Unsafe.As<T, uint>(ref value));
            if (typeof(T) == typeof((uint, uint))) return Create(kind, Unsafe.As<T, (uint, uint)>(ref value));
            if (typeof(T) == typeof((uint, int))) return Create(kind, Unsafe.As<T, (uint, int)>(ref value));

            // Otherwise: box it (works, but allocates)
            AssertPayloadKind(kind, PayloadKind.BoxedValue);
            var e = new EnhancedEnumFlex<TEnum>(kind, PayloadKind.BoxedValue);
            Unsafe.AsRef(in e._ref) = value;
            return e;
        }

        /// <summary>
        /// Deconstructor that exposes both <see cref="Kind"/> and the payload value.
        /// Note: This boxes inline value payloads when called.
        /// </summary>
        /// <param name="kind">Out: the variant kind.</param>
        /// <param name="value">Out: the payload value (boxed for value types).</param>
        public void Deconstruct(out TEnum kind, out object? value)
        {
            kind = Kind;
            value = _payloadKind switch
            {
                PayloadKind.Byte => _inline.Byte,
                PayloadKind.UInt16 => _inline.UInt16,
                PayloadKind.UInt32 => _inline.UInt32,
                PayloadKind.UInt32_UInt32 => (_inline.UInt32_A, _inline.UInt32_B),
                PayloadKind.UInt32_Int32 => (_inline.UInt32_A, _inline.Int32_B),
                PayloadKind.String => _ref ?? throw new InvalidOperationException("String payload is null"),
                PayloadKind.Reference => _ref ?? throw new InvalidOperationException("Reference payload is null"),
                PayloadKind.BoxedValue => _ref ?? throw new InvalidOperationException("Boxed value payload is null"),
                PayloadKind.None => null,
                _ => throw new InvalidOperationException($"Unknown payload kind {_payloadKind}"),
            };
        }

        /// <summary>
        /// True if this instance's <see cref="Kind"/> equals <paramref name="kind"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is(TEnum kind) => Kind.Equals(kind);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte()
        {
            if (_payloadKind != PayloadKind.Byte)
            {
                throw new InvalidOperationException($"Payload is not a byte (actual: {_payloadKind})");
            }
            return _inline.Byte;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetByte(TEnum expectedKind, out byte value)
        {
            if (!Kind.Equals(expectedKind) || _payloadKind != PayloadKind.Byte)
            {
                value = default;
                return false;
            }

            value = _inline.Byte;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetUInt16()
        {
            if (_payloadKind != PayloadKind.UInt16)
            {
                throw new InvalidOperationException($"Payload is not a ushort (actual: {_payloadKind})");
            }
            return _inline.UInt16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetUInt16(TEnum expectedKind, out ushort value)
        {
            if (!Kind.Equals(expectedKind) || _payloadKind != PayloadKind.UInt16)
            {
                value = default;
                return false;
            }

            value = _inline.UInt16;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetUInt32()
        {
            if (_payloadKind != PayloadKind.UInt32)
            {
                throw new InvalidOperationException($"Payload is not a uint (actual: {_payloadKind})");
            }
            return _inline.UInt32;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetUInt32(TEnum expectedKind, out uint value)
        {
            if (!Kind.Equals(expectedKind) || _payloadKind != PayloadKind.UInt32)
            {
                value = default;
                return false;
            }

            value = _inline.UInt32;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (uint a, uint b) GetUInt32UInt32()
        {
            if (_payloadKind != PayloadKind.UInt32_UInt32)
            {
                throw new InvalidOperationException($"Payload is not a (uint,uint) (actual: {_payloadKind})");
            }
            return (_inline.UInt32_A, _inline.UInt32_B);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetUInt32UInt32(TEnum expectedKind, out (uint a, uint b) value)
        {
            if (!Kind.Equals(expectedKind) || _payloadKind != PayloadKind.UInt32_UInt32)
            {
                value = default;
                return false;
            }

            value = (_inline.UInt32_A, _inline.UInt32_B);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (uint a, int b) GetUInt32Int32()
        {
            if (_payloadKind != PayloadKind.UInt32_Int32)
            {
                throw new InvalidOperationException($"Payload is not a (uint,int) (actual: {_payloadKind})");
            }
            return (_inline.UInt32_A, _inline.Int32_B);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetUInt32Int32(TEnum expectedKind, out (uint a, int b) value)
        {
            if (!Kind.Equals(expectedKind) || _payloadKind != PayloadKind.UInt32_Int32)
            {
                value = default;
                return false;
            }

            value = (_inline.UInt32_A, _inline.Int32_B);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetString()
        {
            if (_payloadKind != PayloadKind.String)
            {
                throw new InvalidOperationException($"Payload is not a string (actual: {_payloadKind})");
            }
            return _ref as string ?? throw new InvalidOperationException("String payload is null");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetString(TEnum expectedKind, [NotNullWhen(true)] out string? value)
        {
            if (!Kind.Equals(expectedKind) || _payloadKind != PayloadKind.String)
            {
                value = null;
                return false;
            }

            value = _ref as string;
            return value != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetReference<T>() where T : class
        {
            if (_payloadKind != PayloadKind.Reference)
            {
                throw new InvalidOperationException($"Payload is not a reference of expected kind PayloadKind.Reference (actual: {_payloadKind})");
            }
            return _ref as T ?? throw new InvalidOperationException($"Reference payload is not of expected type {typeof(T).FullName}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetReference<T>(TEnum expectedKind, [NotNullWhen(true)] out T? value) where T : class
        {
            if (!Kind.Equals(expectedKind) || _payloadKind != PayloadKind.Reference)
            {
                value = null;
                return false;
            }

            value = _ref as T;
            return value != null;
        }

        /// <summary>
        /// Get value as a boxed object.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <returns>The payload value cast to T.</returns>
        /// <exception cref="InvalidOperationException">If payload cannot be cast to T.</exception>
        public T Get<T>()
        {
            if (TryGet<T>(Kind, out T value))
            {
                return value;
            }
            throw new InvalidOperationException($"Payload is not a value of expected kind {_payloadKind}");
        }

        /// <summary>
        /// Generic getter that attempts to retrieve the payload as type T.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="expectedKind">The expected enum kind.</param>
        /// <param name="value">Out: the payload value if successful.</param>
        /// <returns>True if kind matches and payload can be cast to T.</returns>
        public bool TryGet<T>(TEnum expectedKind, out T value)
        {
            if (!Kind.Equals(expectedKind))
            {
                value = default!;
                return false;
            }

            if (_payloadKind == PayloadKind.Byte && typeof(T) == typeof(byte))
            {
                value = (T)(object)_inline.Byte;
                return true;
            }

            if (_payloadKind == PayloadKind.UInt16 && typeof(T) == typeof(ushort))
            {
                value = (T)(object)_inline.UInt16;
                return true;
            }

            if (_payloadKind == PayloadKind.UInt32 && typeof(T) == typeof(uint))
            {
                value = (T)(object)_inline.UInt32;
                return true;
            }

            if (_payloadKind == PayloadKind.UInt32_UInt32 && typeof(T) == typeof((uint, uint)))
            {
                value = (T)(object)(_inline.UInt32_A, _inline.UInt32_B);
                return true;
            }

            if (_payloadKind == PayloadKind.UInt32_Int32 && typeof(T) == typeof((uint, int)))
            {
                value = (T)(object)(_inline.UInt32_A, _inline.Int32_B);
                return true;
            }

            if (_payloadKind == PayloadKind.String && typeof(T) == typeof(string) && _ref is string s)
            {
                value = (T)(object)s;
                return true;
            }

            if (_payloadKind == PayloadKind.Reference && _ref is T rt)
            {
                value = rt;
                return true;
            }

            if (_payloadKind == PayloadKind.BoxedValue && _ref is T t)
            {
                value = t;
                return true;
            }

            value = default!;
            return false;
        }
    }
}
