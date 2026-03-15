using System.Reflection;
using System.Runtime.CompilerServices;

namespace Stardust.Utilities
{
    using static Result<string>;

    /// <summary>
    /// Extension methods for byte ordering and other utilities.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns the first attribute of type <typeparamref name="T"/> on the specified <paramref name="type"/>.
        /// When the type was loaded from a different <see cref="System.Runtime.Loader.AssemblyLoadContext"/>,
        /// standard <see cref="MemberInfo.GetCustomAttributes(Type, bool)"/> returns nothing because
        /// the attribute type identity differs across contexts. In that case we fall back to
        /// <see cref="CustomAttributeData"/> metadata and reconstruct the attribute locally.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>The attribute instance, or null if not found.</returns>
        public static T? GetAttribute<T>(this Type type, bool inherit = true) where T : Attribute
        {
            Type attrType = typeof(T);
            if (type == null || attrType == null) return null;

            // Primary: works when attribute and type share the same ALC / type identity
            var result = type.GetCustomAttributes(attrType, inherit: inherit).FirstOrDefault() as T;
            if (result != null) return result;

            // Fallback: reconstruct from CustomAttributeData (works across ALC boundaries)
            return ReconstructAttributeFromMetadata<T>(type.CustomAttributes);
        }

        /// <summary>
        /// Return the first attribute of this type on the field (property) of Type T.
        /// Falls back to <see cref="CustomAttributeData"/> when the type comes from a
        /// different <see cref="System.Runtime.Loader.AssemblyLoadContext"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="fieldName"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static T? GetAttribute<T>(this Type type, string fieldName, bool inherit = true) where T: Attribute
        {
            Type attrType = typeof(T);
            if (type == null || attrType == null || string.IsNullOrEmpty(fieldName)) return null;
            var field = type.GetProperty(fieldName);
            if (field == null) return null;

            var result = field.GetCustomAttributes(attrType, inherit: inherit).FirstOrDefault() as T;
            if (result != null) return result;

            // Fallback: reconstruct from CustomAttributeData (works across ALC boundaries)
            return ReconstructAttributeFromMetadata<T>(field.CustomAttributes);
        }

        /// <summary>
        /// Return true if this class, or a base class, has the [BitFields] attribute.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>True if the type has a <see cref="BitFieldsAttribute"/>.</returns>
        public static bool IsBitFieldsType(this Type type, bool inherit = true)
        {
            // See if this type has the [BitFields] attribute.
            return type.GetAttribute<BitFieldsAttribute>(inherit) != null;
        }

        /// <summary>
        /// Return true if this class, or a base class, has the [BitFieldsView] attribute.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>True if the type has a <see cref="BitFieldsViewAttribute"/>.</returns>
        public static bool IsBitFieldsViewType(this Type type, bool inherit = true)
        {
            // See if this type has the [BitFieldsView] attribute.
            return type.GetAttribute<BitFieldsViewAttribute>(inherit) != null;
        }

        /// <summary>
        /// True if type is a BitFields struct or a BitFieldsView struct (or derives from one).
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>True if the type has a <see cref="BitFieldsAttribute"/> or <see cref="BitFieldsViewAttribute"/>.</returns>
        public static bool IsBitsType(this Type type, bool inherit = true)
        {
            if (type == null) return false;
            return (type.IsBitFieldsType(inherit) || type.IsBitFieldsViewType(inherit));
        }

        /// <summary>
        /// True if this field has a BitFieldAttribute and it is a member of a [BitFields] or
        /// [BitFieldsView] struct.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="fieldName">The property name to check.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>True if the property has a <see cref="BitFieldAttribute"/>.</returns>
        public static bool IsBitField(this Type type, string fieldName, bool inherit = true)
        {
            if (type == null || string.IsNullOrEmpty(fieldName)) return false;
            if (!type.IsBitsType(inherit))  return false;
            return type.GetAttribute<BitFieldAttribute>(fieldName, inherit) != null;
        }

        /// <summary>
        /// True if this field has a BitFlagAttribute and it is a member of a [BitFields] or [BitFieldsView] struct.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="field">The property to check.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>True if the property has a <see cref="BitFlagAttribute"/>.</returns>
        public static bool IsBitFlag(this Type type, PropertyInfo field, bool inherit= true)
        {
            if (type == null || field == null) return false;
            if (!type.IsBitsType(inherit)) return false;
            return type.GetAttribute<BitFlagAttribute>(field.Name, inherit) != null;
        }

        /// <summary>
        /// True if this field has a BitFlagAttribute.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="fieldName">The property name to check.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>True if the property has a <see cref="BitFlagAttribute"/>.</returns>
        public static bool IsBitFlag(this Type type, string fieldName, bool inherit = true)
        {
            if (type == null || string.IsNullOrEmpty(fieldName)) return false;
            if (!type.IsBitsType(inherit)) return false;
            return type.GetAttribute<BitFlagAttribute>(fieldName, inherit) != null;
        }

        private static int GetBitCountForTypeName(string typeName) => typeName switch
        {
            "Byte" or "SByte" => 8,
            "UInt16" or "Int16" => 16,
            "UInt32" or "Int32" => 32,
            "UInt64" or "Int64" => 64,
            "Single" => 32,
            "Double" => 64,
            "Half" => 16,
            "Decimal" => 128,
            "UInt128" or "Int128" => 128,
            _ => 0
        };

        private static int GetBitCountForStorageTypeEnum(int enumValue) => enumValue switch
        {
            0 or 1 => 8,         // Byte, SByte
            2 or 3 => 16,        // Int16, UInt16
            4 or 5 => 32,        // Int32, UInt32
            6 or 7 => 64,        // Int64, UInt64
            8 or 9 => 64,        // NInt, NUInt (treated as 64-bit for metadata)
            10 => 16,            // Half
            11 => 32,            // Single
            12 => 64,            // Double
            13 => 128,           // Decimal
            14 or 15 => 128,     // Int128, UInt128
            _ => 0
        };

        /// <summary>
        /// Get the length of this BitField or BitFlag in bits.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="fieldName">The property name, or null to get the total struct bit count.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>Result with length if success or error message if failure.</returns>
        public static Result<int,string> GetBitLength(this Type type, string? fieldName = null, bool inherit = true)
        {
            if (fieldName != null)
            {
                return type.GetStartAndEndBits(fieldName, inherit).Match(
                    onSuccess: bits => Result<int, string>.Ok(bits.endBit - bits.startBit + 1),
                    onFailure: err => Result<int, string>.Err(err)
                );
            }
            // Bits based on size of struct
            if (!type.IsBitsType(inherit))
            {
                return Result<int, string>.Err("Type is not a BitFields struct or BitFieldsView struct");
            }
            var structTotalBitsRes = type.GetBitTypeAttribute(inherit: inherit).Match(
                onSuccess: attr =>
                {
                    if (attr is BitFieldsAttribute fieldsAttr)
                    {
                        return Result<int, string>.Ok(fieldsAttr.BitCount);
                    }
                    else
                    {
                        return Result<int, string>.Err("Type does not have BitFields attribute");
                    }
                },
                onFailure: err => Result<int, string>.Err(err)
            );
            return structTotalBitsRes;
        }

        /// <summary>
        /// Get the start bit.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="fieldName">The property name.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>Result with start bit number if success or error message if failure.</returns>
        public static Result<int,string> GetStartBit(this Type type, string fieldName, bool inherit = true)
        {
            return type.GetStartAndEndBits(fieldName, inherit).Match(
                onSuccess: bits => Result<int, string>.Ok(bits.startBit),
                onFailure: err => Result<int, string>.Err(err)
            );
        }

        /// <summary>
        /// Get the end bit.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="fieldName">The property name.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>Result with end bit number if success or error message if failure.</returns>
        public static Result<int,string> GetEndBit(this Type type, string fieldName, bool inherit = true)
        {
            return type.GetStartAndEndBits(fieldName, inherit).Match(
                onSuccess: bits => Result<int, string>.Ok(bits.endBit),
                onFailure: err => Result<int, string>.Err(err)
            );
        }

        /// <summary>
        /// Get start and end bits for this BitField.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="fieldName">The property name.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>Result with start and end bits if success or error message if failure.</returns>
        public static Result<(int startBit,int endBit),string> GetStartAndEndBits(this Type type, string fieldName, bool inherit = true)
        {
            if (type == null)
            {
                return Result<(int startBit, int endBit), string>.Err("'type' is null");
            }               
            if (!type.IsBitsType())
            {
                return Result<(int startBit, int endBit), string>.Err("Type is not a BitFields struct or BitFieldsView struct");
            }
            if (string.IsNullOrEmpty(fieldName))
            {
                return Result<(int startBit, int endBit), string>.Err("No field name");
            }
            var field = type.GetProperty(fieldName);
            if (field == null)
            {
                return Result<(int startBit, int endBit), string>.Err("Field not found");
            }
            BitFieldAttribute? fieldAttr = type.GetAttribute<BitFieldAttribute>(fieldName, inherit);
            if (fieldAttr != null)
            {
                return Ok((fieldAttr.StartBit, fieldAttr.EndBit));
            }
            BitFlagAttribute? flagAttr = type.GetAttribute<BitFlagAttribute>(fieldName, inherit);
            if (flagAttr != null)
            {
                return Ok((flagAttr.Bit, flagAttr.Bit));
            }

            return Result<(int startBit, int endBit), string>.Err("Field is not a BitField or a BitFlag");
        }

        /// <summary>
        /// Get the <see cref="MustBe"/> override for this field if it has a BitField or BitFlag attribute.
        /// If the field does not have a BitField or BitFlag attribute, or if the type is not a
        /// BitFields struct or BitFieldsView struct, return an error.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="fieldName">The property name.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>A result containing the <see cref="MustBe"/> value, or an error string on failure.</returns>
        public static Result<MustBe, string> GetFieldValueOverride(this Type type, string fieldName, bool inherit = true)
        {
            if (type == null)
            {
                return Result<MustBe, string>.Err("'type' cannot be null");
            }
            if (string.IsNullOrEmpty(fieldName))
            {
                return Result<MustBe, string>.Err("'fieldName' must not be empty");
            }
            var fieldAttr = type.GetAttribute<BitFieldAttribute>(inherit);
            if (fieldAttr != null)
            {
                var mustBe = fieldAttr.ValueOverride;
                return Ok(mustBe);
            }
            var flagAttr = type.GetAttribute<BitFlagAttribute>(inherit);
            if (flagAttr != null)
            {
                var mustBe = flagAttr.ValueOverride;
                return Ok(mustBe);
            }
            return Result<MustBe, string>.Err("Field does not have BitField or BitFlag attribute");
        }

        /// <summary>
        /// Get the value for undefined bits in a struct.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static Result<UndefinedBitsMustBe,string> GetUndefinedBitsMustBe(this Type type, bool inherit = true)
        {
            if (type == null)
            {
                return Result<UndefinedBitsMustBe, string>.Err("type is null");
            }
            if (!type.IsBitFieldsType() && !type.IsBitFieldsViewType())
            {
                return Result<UndefinedBitsMustBe, string>.Err("Type is not a BitFields struct or BitFieldsView struct");
            }
            var fieldsAttr = type.GetAttribute<BitFieldsAttribute>(inherit);
            if (fieldsAttr != null)
            {
                return Ok(fieldsAttr.UndefinedBits);
            }
            else
            {
                return Result<UndefinedBitsMustBe, string>.Err("Type does not have BitFields attribute");
            }
        }
        /// <summary>
        /// Get the byte and bit order for this bit struct or bit struct view.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>Byte order and bit order, or an error message on failure.</returns>
        public static Result<(ByteOrder byteOrder, BitOrder bitOrder),string> GetBitAndByteOrder(this Type type, bool inherit = true)
        {
            if (type == null)
            {
                return Result<(ByteOrder byteOrder, BitOrder bitOrder), string>.Err("type is null");
            }
            var fieldsAttr = type.GetAttribute<BitFieldsAttribute>(inherit);
            if (fieldsAttr != null)
            {
                return Ok((fieldsAttr.ByteOrder, fieldsAttr.BitOrder));
            }
            var viewAttr = type.GetAttribute<BitFieldsViewAttribute>(inherit);
            if (viewAttr != null)
            {
                return Ok((viewAttr.ByteOrder, viewAttr.BitOrder));
            }
            else
            {
                return Result<(ByteOrder byteOrder, BitOrder bitOrder), string>.Err("Type does not have BitFields or BitFieldsView attribute");
            }
        }

        /// <summary>
        /// Get the description of the type or field.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="field">Optional property name. When null, returns the type-level description.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>Description (may be null) of the type if field is null, else field description (may be null).
        /// Error if BitFields, BitFieldsView, BitField, or BitFlag attribute is not found.
        /// </returns>
        public static Result<(string? description, Type? descriptionResourceType), string> GetBitsDescription(this Type type, string? field = null, bool inherit = true)
        {
            if (type == null)
            {
                return Result<(string? description, Type? descriptionResourceType), string>.Err("type is null");
            }

            var fieldInfo = field != null ? type.GetProperty(field) : null;
            if (fieldInfo == null && field != null)
            {
                return Result<(string? description, Type? descriptionResourceType), string>.Err("Invalid field name");
            }
            // Return description of the struct if no field, otherwise return the description of the field.
            if (fieldInfo == null)
            {
                // Return the description of the struct (type).
                var fldsAttr = type.GetAttribute<BitFieldsAttribute>(inherit);
                if (fldsAttr != null)
                {
                    return Ok((fldsAttr.Description, fldsAttr.DescriptionResourceType));
                }
                var fldsViewAttr = type.GetAttribute<BitFieldsViewAttribute>(inherit);
                if (fldsViewAttr != null)
                {
                    return Ok((fldsViewAttr.Description, fldsViewAttr.DescriptionResourceType));
                }
                return Result<(string? description, Type? descriptionResourceType), string>.Err("Type does not have BitFields or BitFieldsView attribute");
            }
            // We have a field, so get the description of the field.
            var fldAttr = type.GetAttribute<BitFieldAttribute>(field!, inherit);
            if (fldAttr != null)
            {
                return Ok((fldAttr.Description, fldAttr.DescriptionResourceType));
            }
            var flagAttr = type.GetAttribute<BitFlagAttribute>(field!, inherit);
            if (flagAttr != null)
            {
                return Ok((flagAttr.Description, flagAttr.DescriptionResourceType));
            }
            return Result<(string? description, Type? descriptionResourceType), string>.Err($"Field {type.FullName}.{field} does not have a BitField or BitFlag attribute");
        }

        /// <summary>
        /// Retrieves the <c>[BitFields]</c>, <c>[BitFieldsView]</c>, <c>[BitField]</c>, or <c>[BitFlag]</c> attribute
        /// from the specified type or one of its properties.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="field">Optional property name. When null, returns the type-level attribute.</param>
        /// <param name="inherit">When true, searches inherited attributes.</param>
        /// <returns>A successful result containing the attribute, or an error string on failure.</returns>
        public static Result<Attribute,string> GetBitTypeAttribute(this Type type, string? field = null, bool inherit = true)
        {
            if (type == null)
            {
                return Result<Attribute, string>.Err("type is null");
            }
            var fieldInfo = field != null ? type.GetProperty(field) : null;
            if (fieldInfo == null && field != null)
            {
                return Result<Attribute, string>.Err("Invalid field name");
            }
            Attribute? attribute;
            if (fieldInfo == null)
            {
                // Return the attribute of the struct (type).
                attribute = type.GetAttribute<BitFieldsAttribute>(inherit);
                attribute ??= type.GetAttribute<BitFieldsViewAttribute>(inherit);
                if (attribute != null)
                {
                    return Ok(attribute);
                }
                return Result<Attribute, string>.Err($"Type {type.FullName} does not have a BitFields or BitFieldsView attribute");
            }

            // We have a field, so get the attribute of the field.
            attribute = type.GetAttribute<BitFieldAttribute>(field!, inherit);
            attribute ??= type.GetAttribute<BitFlagAttribute>(field!, inherit);

            return attribute != null ? Ok(attribute) : Result<Attribute, string>.Err($"Field {type.FullName}.{field} does not have a BitField or BitFlag attribute");
        }

        // ── Attribute-metadata discovery ────────────────────────────────

        /// <summary>
        /// Discovers <see cref="BitFieldInfo"/> metadata for a <c>[BitFields]</c> or <c>[BitFieldsView]</c>
        /// type by reading <see cref="CustomAttributeData"/> directly, without invoking any generated code.
        /// This works reliably across <see cref="System.Runtime.Loader.AssemblyLoadContext"/> boundaries
        /// where the generated <c>Fields</c> property cannot be called via delegates due to type-identity
        /// mismatches.
        /// </summary>
        /// <param name="type">A struct type decorated with <c>[BitFields]</c> or <c>[BitFieldsView]</c>.</param>
        /// <returns>
        /// An array of <see cref="BitFieldInfo"/> describing each declared field/flag, sorted by start bit.
        /// Returns an empty array if the type has no recognised bit-field attributes.
        /// </returns>
        public static BitFieldInfo[] GetBitFieldInfoFromAttributes(this Type type)
        {
            if (type == null) return [];

            var structInfo = ReadStructAttributeMetadata(type);
            if (!structInfo.Found) return [];

            return ReadFieldInfoFromPropertyMetadata(
                type,
                structInfo.BitOrder,
                structInfo.ByteOrder,
                structInfo.TotalBits,
                structInfo.UndefinedMustBe,
                structInfo.Description);
        }

        /// <summary>
        /// Retrieves the <c>Fields</c> metadata from a <c>[BitFields]</c> or <c>[BitFieldsView]</c>
        /// type. First attempts to invoke the generated static <c>Fields</c> property via a typed
        /// delegate (fastest path, works when the type is in the same
        /// <see cref="System.Runtime.Loader.AssemblyLoadContext"/>). Falls back to
        /// <see cref="GetBitFieldInfoFromAttributes"/> when the type was loaded from a different
        /// context (cross-ALC scenario).
        /// </summary>
        /// <param name="type">A struct type decorated with <c>[BitFields]</c> or <c>[BitFieldsView]</c>.</param>
        /// <returns>A successful result containing the field metadata array, or an error string on failure.</returns>
        public static Result<BitFieldInfo[], string> GetFieldInfo(this Type type)
        {
            if (type == null)
                return Result<BitFieldInfo[], string>.Err("'type' is null");

            var prop = type.GetProperty("Fields", BindingFlags.Public | BindingFlags.Static);
            if (prop != null)
            {
                // The generated property returns ReadOnlySpan<BitFieldInfo> backed by an array literal.
                // ReadOnlySpan cannot be obtained via PropertyInfo.GetValue (reflection limitation).
                // Instead, invoke the getter via a typed delegate.
                var getter = prop.GetGetMethod();
                if (getter != null)
                {
                    var delegateType = typeof(SpanGetter<>).MakeGenericType(typeof(BitFieldInfo));
                    var del = Delegate.CreateDelegate(delegateType, getter, throwOnBindFailure: false);
                    if (del != null)
                    {
                        var span = ((SpanGetter<BitFieldInfo>)del)();
                        return Ok(span.ToArray());
                    }

                    // Fallback: try direct GetValue for non-span return types
                    try
                    {
                        var value = prop.GetValue(null);
                        if (value is BitFieldInfo[] array)
                            return Ok(array);
                    }
                    catch (NotSupportedException) { /* ReadOnlySpan<T> cannot be boxed — expected for cross-ALC types */ }
                }
            }

            // Last resort: reconstruct from CustomAttributeData (works across ALC boundaries)
            var fromAttrs = type.GetBitFieldInfoFromAttributes();
            if (fromAttrs.Length > 0)
                return Ok(fromAttrs);

            return Result<BitFieldInfo[], string>.Err(
                $"Type '{type.Name}' does not expose readable bit-field metadata. " +
                $"Ensure it is decorated with [BitFields] or [BitFieldsView].");
        }

        private delegate ReadOnlySpan<T> SpanGetter<T>();

        private readonly record struct StructAttributeMetadata(
            BitOrder BitOrder,
            ByteOrder ByteOrder,
            int TotalBits,
            UndefinedBitsMustBe UndefinedMustBe,
            string? Description,
            bool Found);

        /// <summary>
        /// Reads struct-level <c>[BitFields]</c> or <c>[BitFieldsView]</c> metadata from
        /// <see cref="CustomAttributeData"/>, which works across ALC boundaries.
        /// </summary>
        private static StructAttributeMetadata ReadStructAttributeMetadata(Type type)
        {
            foreach (var cad in type.CustomAttributes)
            {
                string attrName = cad.AttributeType.Name;

                if (attrName == nameof(BitFieldsAttribute))
                {
                    var bitOrder = BitOrder.BitZeroIsLsb;
                    var byteOrder = ByteOrder.LittleEndian;
                    var undefinedMustBe = UndefinedBitsMustBe.Any;
                    int totalBits = 0;
                    string? description = null;

                    if (cad.ConstructorArguments.Count > 0)
                    {
                        var first = cad.ConstructorArguments[0];
                        if (first.Value is Type storageType)
                            totalBits = GetBitCountForTypeName(storageType.Name);
                        else if (first.ArgumentType.Name == nameof(StorageType) && first.Value is int enumValue)
                            totalBits = GetBitCountForStorageTypeEnum(enumValue);
                        else if (first.Value is int bitCount)
                            totalBits = bitCount;
                    }

                    for (int i = 1; i < cad.ConstructorArguments.Count; i++)
                    {
                        var arg = cad.ConstructorArguments[i];
                        if (arg.ArgumentType.Name == nameof(BitOrder))
                            bitOrder = (BitOrder)(int)arg.Value!;
                        else if (arg.ArgumentType.Name == nameof(ByteOrder))
                            byteOrder = (ByteOrder)(int)arg.Value!;
                        else if (arg.ArgumentType.Name == nameof(UndefinedBitsMustBe))
                            undefinedMustBe = (UndefinedBitsMustBe)(int)arg.Value!;
                    }

                    description = GetNamedArgString(cad, nameof(BitFieldsAttribute.Description));

                    return new(bitOrder, byteOrder, totalBits, undefinedMustBe, description, true);
                }

                if (attrName == nameof(BitFieldsViewAttribute))
                {
                    var bitOrder = BitOrder.BitZeroIsLsb;
                    var byteOrder = ByteOrder.LittleEndian;
                    string? description = null;

                    for (int i = 0; i < cad.ConstructorArguments.Count; i++)
                    {
                        var arg = cad.ConstructorArguments[i];
                        if (arg.ArgumentType.Name == nameof(ByteOrder))
                            byteOrder = (ByteOrder)(int)arg.Value!;
                        else if (arg.ArgumentType.Name == nameof(BitOrder))
                            bitOrder = (BitOrder)(int)arg.Value!;
                    }

                    description = GetNamedArgString(cad, nameof(BitFieldsViewAttribute.Description));

                    return new(bitOrder, byteOrder, 0, UndefinedBitsMustBe.Any, description, true);
                }
            }

            return default;
        }

        /// <summary>
        /// Reads property-level <c>[BitField]</c> and <c>[BitFlag]</c> metadata from
        /// <see cref="CustomAttributeData"/> and builds <see cref="BitFieldInfo"/> records.
        /// </summary>
        private static BitFieldInfo[] ReadFieldInfoFromPropertyMetadata(
            Type type,
            BitOrder structBitOrder,
            ByteOrder structByteOrder,
            int structTotalBits,
            UndefinedBitsMustBe structUndefinedMustBe,
            string? structDescription)
        {
            var fields = new List<BitFieldInfo>();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (var cad in prop.CustomAttributes)
                {
                    string attrName = cad.AttributeType.Name;

                    if (attrName == nameof(BitFieldAttribute) && cad.ConstructorArguments.Count >= 2)
                    {
                        int startBit = (int)cad.ConstructorArguments[0].Value!;
                        int endBit = (int)cad.ConstructorArguments[1].Value!;
                        MustBe mustBe = cad.ConstructorArguments.Count > 2 ? (MustBe)(int)cad.ConstructorArguments[2].Value! : MustBe.Any;
                        string? desc = GetNamedArgString(cad, nameof(BitFieldAttribute.Description));

                        fields.Add(new BitFieldInfo(
                            Name: prop.Name,
                            StartBit: startBit,
                            BitLength: endBit - startBit + 1,
                            PropertyType: MapPropertyTypeName(prop.PropertyType),
                            IsFlag: false,
                            ByteOrder: structByteOrder,
                            BitOrder: structBitOrder,
                            Description: desc,
                            StructTotalBits: structTotalBits,
                            FieldMustBe: mustBe,
                            StructUndefinedMustBe: structUndefinedMustBe,
                            StructDescription: structDescription
                        ));
                    }
                    else if (attrName == nameof(BitFlagAttribute) && cad.ConstructorArguments.Count >= 1)
                    {
                        int bit = (int)cad.ConstructorArguments[0].Value!;
                        MustBe mustBe = cad.ConstructorArguments.Count > 1 ? (MustBe)(int)cad.ConstructorArguments[1].Value! : MustBe.Any;
                        string? desc = GetNamedArgString(cad, nameof(BitFlagAttribute.Description));

                        fields.Add(new BitFieldInfo(
                            Name: prop.Name,
                            StartBit: bit,
                            BitLength: 1,
                            PropertyType: "bool",
                            IsFlag: true,
                            ByteOrder: structByteOrder,
                            BitOrder: structBitOrder,
                            Description: desc,
                            StructTotalBits: structTotalBits,
                            FieldMustBe: mustBe,
                            StructUndefinedMustBe: structUndefinedMustBe,
                            StructDescription: structDescription
                        ));
                    }
                }
            }

            if (structTotalBits == 0 && fields.Count > 0)
            {
                int maxBit = fields.Max(f => f.EndBit);
                int inferred = ((maxBit + 8) / 8) * 8;
                for (int i = 0; i < fields.Count; i++)
                    fields[i] = fields[i] with { StructTotalBits = inferred };
            }

            fields.Sort((a, b) => a.StartBit.CompareTo(b.StartBit));
            return fields.ToArray();
        }

        private static string? GetNamedArgString(CustomAttributeData cad, string name)
        {
            foreach (var na in cad.NamedArguments)
            {
                if (na.MemberName == name && na.TypedValue.Value is string s)
                    return s;
            }
            return null;
        }

        private static string MapPropertyTypeName(Type type)
        {
            return type.Name switch
            {
                "Boolean" => "bool",
                "Byte"    => "byte",
                "SByte"   => "sbyte",
                "UInt16"  => "ushort",
                "Int16"   => "short",
                "UInt32"  => "uint",
                "Int32"   => "int",
                "UInt64"  => "ulong",
                "Int64"   => "long",
                _ when type.IsEnum => MapPropertyTypeName(type.GetEnumUnderlyingType()),
                _ => type.Name.ToLowerInvariant()
            };
        }

        // ── Attribute reconstruction ────────────────────────────────

        /// <summary>
        /// Reconstructs an attribute instance from <see cref="CustomAttributeData"/> metadata.
        /// This enables attribute access across <see cref="System.Runtime.Loader.AssemblyLoadContext"/>
        /// boundaries where standard <see cref="MemberInfo.GetCustomAttributes(Type, bool)"/> returns
        /// nothing because the attribute type loaded in a foreign ALC has a different type identity
        /// than the one in the host ALC.
        /// </summary>
        private static T? ReconstructAttributeFromMetadata<T>(IEnumerable<CustomAttributeData> attributes) where T : Attribute
        {
            string targetName = typeof(T).Name;
            var cad = attributes.FirstOrDefault(c => c.AttributeType.Name == targetName);
            if (cad == null) return null;

            try
            {
                var cadArgs = cad.ConstructorArguments;

                foreach (var ctor in typeof(T).GetConstructors())
                {
                    var parameters = ctor.GetParameters();
                    if (parameters.Length < cadArgs.Count) continue;

                    var invokeArgs = new object?[parameters.Length];
                    bool compatible = true;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (i < cadArgs.Count)
                        {
                            invokeArgs[i] = CoerceToParameterType(cadArgs[i].Value, parameters[i].ParameterType);
                        }
                        else if (parameters[i].HasDefaultValue)
                        {
                            invokeArgs[i] = parameters[i].DefaultValue;
                        }
                        else
                        {
                            compatible = false;
                            break;
                        }
                    }

                    if (!compatible) continue;

                    try
                    {
                        var attr = (T)ctor.Invoke(invokeArgs);

                        foreach (var na in cad.NamedArguments)
                        {
                            var prop = typeof(T).GetProperty(na.MemberName);
                            if (prop != null && prop.CanWrite)
                                prop.SetValue(attr, CoerceToParameterType(na.TypedValue.Value, prop.PropertyType));
                        }

                        return attr;
                    }
                    catch { /* constructor mismatch, try next */ }
                }
            }
            catch { /* metadata not readable */ }

            return null;
        }

        /// <summary>
        /// Coerces a value from <see cref="CustomAttributeData"/> to the target parameter type.
        /// Enum values in metadata are stored as their underlying integer type and must be
        /// converted to the local enum type via <see cref="Enum.ToObject(Type, object)"/>.
        /// </summary>
        private static object? CoerceToParameterType(object? value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsEnum) return Enum.ToObject(targetType, value);
            if (targetType.IsInstanceOfType(value)) return value;
            try { return Convert.ChangeType(value, targetType); }
            catch { return value; }
        }

        /// <summary>
        /// Least-significant byte.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Lo(this ushort value)
        {
            return (byte)(value & 0xff);
        }

        /// <summary>
        /// Least-significant byte.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Lo(this short value)
        {
            return (byte)(value & 0xff);
        }

        /// <summary>
        /// Least-significant ushort.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant ushort.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Lo(this uint value)
        {
            return (ushort)(value & 0xffff);
        }

        /// <summary>
        /// Least-significant byte.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Lo(this UInt16Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant UInt32Be.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant UInt16Be.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be Lo(this UInt32Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant byte.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Lo(this Int16Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant UInt32Be.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant UInt16Be.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be Lo(this Int32Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant uint (lower 32 bits).
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant uint.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Lo(this ulong value)
        {
            return (uint)(value & 0xFFFFFFFF);
        }

        /// <summary>
        /// Least-significant uint (lower 32 bits).
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant uint.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Lo(this long value)
        {
            return (uint)(value & 0xFFFFFFFF);
        }

        /// <summary>
        /// Least-significant UInt32Be.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant UInt32Be.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be Lo(this UInt64Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant UInt32Be.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant UInt32Be.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be Lo(this Int64Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant ushort.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The least-significant ushort.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Lo(this int value)
        {
            return (ushort)(value & 0xffff);
        }

        /// <summary>
        /// Returns a value with the low byte replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low byte to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort SetLo(this ushort value, byte lo)
        {
            return (ushort)((value & 0xff00) | lo);
        }

        /// <summary>
        /// Returns a value with the low byte replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low byte to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short SetLo(this short value, byte lo)
        {
            return (short)(((ushort)(value & 0xff00)) | lo);
        }

        /// <summary>
        /// Returns a value with the low ushort replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low ushort to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetLo(this uint value, ushort lo)
        {
            return (value & 0xffff0000) | lo;
        }

        /// <summary>
        /// Returns a value with the low ushort replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low ushort to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SetLo(this int value, ushort lo)
        {
            return (int)((value & 0xffff0000) | lo);
        }

        /// <summary>
        /// Returns a value with the low byte replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low byte to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be SetLo(this UInt16Be value, byte lo)
        {
            return (value & 0xff00) | ((ushort)lo);
        }

        /// <summary>
        /// Returns a value with the low ushort replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low ushort to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be SetLo(this UInt32Be value, ushort lo)
        {
            return ((value & 0xffff0000) | ((uint)lo));
        }

        /// <summary>
        /// Returns a value with the low byte replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low byte to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16Be SetLo(this Int16Be value, byte lo)
        {
            return (Int16Be)(ushort)(((ushort)(value & 0xff00)) | lo);
        }

        /// <summary>
        /// Returns a value with the low ushort replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low ushort to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Be SetLo(this Int32Be value, ushort lo)
        {
            return (Int32Be)((value & 0xffff0000) | lo);
        }

        /// <summary>
        /// Set least-significant uint (lower 32 bits).
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low uint to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SetLo(this ulong value, uint lo)
        {
            return (value & 0xFFFFFFFF00000000) | lo;
        }

        /// <summary>
        /// Set least-significant uint (lower 32 bits).
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low uint to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SetLo(this long value, uint lo)
        {
            return (long)((ulong)(value & unchecked((long)0xFFFFFFFF00000000)) | lo);
        }

        /// <summary>
        /// Set least-significant UInt32Be.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low UInt32Be to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64Be SetLo(this UInt64Be value, UInt32Be lo)
        {
            return new UInt64Be(value.hi, lo);
        }

        /// <summary>
        /// Set least-significant UInt32Be.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="lo">The low UInt32Be to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64Be SetLo(this Int64Be value, UInt32Be lo)
        {
            return new Int64Be(value.hi, lo);
        }

        /// <summary>
        /// Most significant byte.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Hi(this ushort value)
        {
            return (byte)(value >> 8);
        }

        /// <summary>
        /// Most significant byte.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Hi(this short value)
        {
            return (byte)(value >> 8);
        }

        /// <summary>
        /// Most-significant ushort.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant ushort.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Hi(this uint value)
        {
            return (ushort)(value >> 16);
        }

        /// <summary>
        /// Most-significant ushort.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant ushort.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Hi(this int value)
        {
            return (ushort)((uint)value >> 16);
        }

        /// <summary>
        /// Most significant byte.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Hi(this UInt16Be value)
        {
            return value.hi;
        }

        /// <summary>
        /// Most-significant UInt16Be.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant UInt16Be.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be Hi(this UInt32Be value)
        {
            return value.hi;
        }

        /// <summary>
        /// Most significant byte.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Hi(this Int16Be value)
        {
            return value.hi;
        }

        /// <summary>
        /// Most-significant UInt16Be.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant UInt16Be.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be Hi(this Int32Be value)
        {
            return value.hi;
        }

        /// <summary>
        /// Most-significant uint (upper 32 bits).
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant uint.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Hi(this ulong value)
        {
            return (uint)(value >> 32);
        }

        /// <summary>
        /// Most-significant uint (upper 32 bits).
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant uint.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Hi(this long value)
        {
            return (uint)((ulong)value >> 32);
        }

        /// <summary>
        /// Most-significant UInt32Be.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant UInt32Be.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be Hi(this UInt64Be value)
        {
            return value.hi;
        }

        /// <summary>
        /// Most-significant UInt32Be.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The most-significant UInt32Be.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be Hi(this Int64Be value)
        {
            return value.hi;
        }

        /// <summary>
        /// Returns a value with the high byte replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="hi">The high byte to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort SetHi(this ushort value, byte hi)
        {
            return (ushort)((value & 0x00ff) | hi << 8);
        }

        /// <summary>
        /// Returns a value with the high byte replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="hi">The high byte to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short SetHi(this short value, byte hi)
        {
            return (short)((value & 0x00ff) | hi << 8);
        }

        /// <summary>
        /// Returns a value with the high ushort replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="hi">The high ushort to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetHi(this uint value, ushort hi)
        {
            return (value & 0x0000ffff) | ((uint)hi << 16);
        }

        /// <summary>
        /// Returns a value with the high ushort replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="hi">The high ushort to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SetHi(this int value, ushort hi)
        {
            return (int)((uint)(value & 0x0000ffff) | (uint)hi << 16);
        }

        /// <summary>
        /// Returns a value with the high byte replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="hi">The high byte to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be SetHi(this UInt16Be value, byte hi)
        {
            return (UInt16Be)(((ushort)value & 0x00ff) | hi << 8);
        }

        /// <summary>
        /// Returns a value with the high byte replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="hi">The high byte to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16Be SetHi(this Int16Be value, byte hi)
        {
            return (Int16Be)(ushort)(((ushort)value & 0x00ff) | hi << 8);
        }

        /// <summary>
        /// Returns a value with the high Int16Be replaced.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="hi">The high Int16Be to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Be SetHi(this Int32Be value, Int16Be hi)
        {
            return (value & 0x0000ffff) | ((Int32Be)hi << 16);
        }

        /// <summary>
        /// Set most-significant uint (upper 32 bits).
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="hi">The high uint to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SetHi(this ulong value, uint hi)
        {
            return (value & 0x00000000FFFFFFFF) | ((ulong)hi << 32);
        }

        /// <summary>
        /// Set most-significant uint (upper 32 bits).
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="hi">The high uint to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SetHi(this long value, uint hi)
        {
            return (long)((ulong)(value & 0x00000000FFFFFFFF) | ((ulong)hi << 32));
        }

        /// <summary>
        /// Set most-significant UInt32Be.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="hi">The high UInt32Be to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64Be SetHi(this UInt64Be value, UInt32Be hi)
        {
            return new UInt64Be(hi, value.lo);
        }

        /// <summary>
        /// Set most-significant UInt32Be.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="hi">The high UInt32Be to set.</param>
        /// <returns>The updated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64Be SetHi(this Int64Be value, UInt32Be hi)
        {
            return new Int64Be(hi, value.lo);
        }

        /// <summary>
        /// Subtracts two values, returning MinValue on underflow or MaxValue on overflow (for signed types).
        /// For unsigned types, returns 0 if the result would underflow.
        /// </summary>
        /// <param name="a">The value to subtract from.</param>
        /// <param name="b">The value to subtract.</param>
        /// <returns>The saturated difference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SaturatingSub(this int a, int b)
        {
            int result = a - b;
            // Check for overflow: if signs of a and b differ and result has wrong sign
            if (((a ^ b) & (a ^ result)) < 0)
            {
                // Overflow occurred: return appropriate limit
                return a < 0 ? int.MinValue : int.MaxValue;
            }
            return result;
        }

        /// <summary>
        /// Subtracts two values, clamping on overflow.
        /// </summary>
        /// <param name="a">The value to subtract from.</param>
        /// <param name="b">The value to subtract.</param>
        /// <returns>The saturated difference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SaturatingSub(this long a, long b)
        {
            long result = a - b;
            // Check for overflow: if signs of a and b differ and result has wrong sign
            if (((a ^ b) & (a ^ result)) < 0)
            {
                // Overflow occurred: return appropriate limit
                return a < 0 ? long.MinValue : long.MaxValue;
            }
            return result;
        }

        /// <summary>
        /// Subtracts two values, clamping to zero on underflow.
        /// </summary>
        /// <param name="a">The value to subtract from.</param>
        /// <param name="b">The value to subtract.</param>
        /// <returns>The saturated difference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SaturatingSub(this uint a, uint b)
        {
            // For unsigned, just check if b > a (would underflow)
            return b > a ? uint.MinValue : a - b;
        }

        /// <summary>
        /// Subtracts two values, clamping to zero on underflow.
        /// </summary>
        /// <param name="a">The value to subtract from.</param>
        /// <param name="b">The value to subtract.</param>
        /// <returns>The saturated difference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SaturatingSub(this ulong a, ulong b)
        {
            // For unsigned, just check if b > a (would underflow)
            return b > a ? ulong.MinValue : a - b;
        }

        /// <summary>
        /// Adds two values, clamping on overflow.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The saturated sum.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SaturatingAdd(this int a, int b)
        {
            int result = a + b;
            // Check for overflow: if signs of a and b are same and result has different sign
            if (((a ^ result) & (b ^ result)) < 0)
            {
                // Overflow occurred: return appropriate limit
                return a < 0 ? int.MinValue : int.MaxValue;
            }
            return result;
        }

        /// <summary>
        /// Adds two values, clamping on overflow.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The saturated sum.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SaturatingAdd(this long a, long b)
        {
            long result = a + b;
            // Check for overflow: if signs of a and b are same and result has different sign
            if (((a ^ result) & (b ^ result)) < 0)
            {
                // Overflow occurred: return appropriate limit
                return a < 0 ? long.MinValue : long.MaxValue;
            }
            return result;
        }

        /// <summary>
        /// Adds two values, clamping on overflow.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The saturated sum.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SaturatingAdd(this uint a, uint b)
        {
            uint result = a + b;
            // For unsigned, overflow if result < a (wrapped around)
            return result < a ? uint.MaxValue : result;
        }

        /// <summary>
        /// Adds two values, clamping on overflow.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The saturated sum.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SaturatingAdd(this ulong a, ulong b)
        {
            ulong result = a + b;
            // For unsigned, overflow if result < a (wrapped around)
            return result < a ? ulong.MaxValue : result;
        }
    }
}
