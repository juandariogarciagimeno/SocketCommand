// --------------------------------------------------------------------------------------------------
// <copyright file="DefaultSocketMessageSerializer.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Defaults;

using SocketCommand.Abstractions.Attributes;
using SocketCommand.Abstractions.Interfaces;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Default implementation for object serialization. serializes to byte.
/// </summary>
public sealed class DefaultSocketMessageSerializer : ISocketMessageSerializer
{
    /// <summary>
    /// Deserializes the byte array to the specified type.
    /// </summary>
    /// <param name="data">Data to deserialize.</param>
    /// <param name="type">Type to deserialize into.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="ArgumentException">If provided data is not decorated with the needed attributes. <see cref="SocketMessageAttribute"/> and <see cref="OrderAttribute"/>.</exception>
    public object? Deserialize(byte[] data, Type type)
    {
        var attr = type.GetCustomAttributes(typeof(SocketMessageAttribute), true).FirstOrDefault() as SocketMessageAttribute;

        if (attr is null)
        {
            throw new ArgumentException($"Type {type.Name} is not decorated as serializable. Please add the attribute [{nameof(SocketMessageAttribute)}] to your type and [{nameof(OrderAttribute)}] to the properties to serialize.", nameof(data));
        }

        using var memstr = new MemoryStream(data);
        var reader = new BinaryReader(memstr);
        var deserialized = DeserializeObject(type, ref reader);

        reader.Close();
        reader.Dispose();
        return deserialized;
    }

    /// <summary>
    /// Serializes the data into a byte array.
    /// </summary>
    /// <typeparam name="T">Type of the data to serialize.</typeparam>
    /// <param name="data">object to serialize.</param>
    /// <returns>Serialized data.</returns>
    /// <exception cref="ArgumentException">If provided data is not decorated with the needed attributes. <see cref="SocketMessageAttribute"/> and <see cref="OrderAttribute"/>.</exception>
    public byte[] Serialize<T>(T data)
    {
        return Serialize((object)data!);
    }

    /// <summary>
    /// Serializes the data into a byte array.
    /// </summary>
    /// <param name="data">object to serialize.</param>
    /// <returns>Serialized data.</returns>
    /// <exception cref="ArgumentException">If provided data is not decorated with the needed attributes. <see cref="SocketMessageAttribute"/> and <see cref="OrderAttribute"/>.</exception>
    public byte[] Serialize(object data)
    {
        var type = data.GetType();
        var attr = type.GetCustomAttributes(typeof(SocketMessageAttribute), true).FirstOrDefault() as SocketMessageAttribute;

        if (attr is null)
        {
            throw new ArgumentException($"Type {type.Name} is not decorated as serializable. Please add the attribute [{nameof(SocketMessageAttribute)}] to your type and [{nameof(OrderAttribute)}] to the properties to serialize.", nameof(data));
        }

        using var memstr = new MemoryStream();
        var writer = new BinaryWriter(memstr);
        SerializeObject(type, data, ref writer);

        writer.Flush();
        writer.Dispose();
        return memstr.ToArray();
    }

    /// <summary>
    /// Runs through properties decorated with <see cref="OrderAttribute"/> and serializes them recursively.
    /// </summary>
    /// <param name="type">Type being serialized.</param>
    /// <param name="o">Instance of the type.</param>
    /// <param name="writer">Binary Writer.</param>
    private void SerializeObject(Type type, object o, ref BinaryWriter writer)
    {
        var properties = type.GetProperties();
        var orderedProperties = properties
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.GetCustomAttributes(typeof(OrderAttribute), true).Length != 0)
            .OrderBy(p => ((OrderAttribute)p.GetCustomAttributes(typeof(OrderAttribute), true)!.FirstOrDefault()!)!.Order);

        foreach (var property in orderedProperties)
        {
            var value = property.GetValue(o);
            if (property.PropertyType.IsValueTypeOrString())
            {
                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (value is null)
                    {
                        writer.Write((byte)0);
                    }
                    else
                    {
                        SerializeValueType(value, ref writer);
                    }
                }
                else
                {
                    SerializeValueType(value!, ref writer);
                }
            }
            else if (property.PropertyType.IsCollection())
            {
                SerializeArray(value!, ref writer);
            }
            else
            {
                if (value is null)
                {
                    writer.Write((byte)0);
                }
                else
                {
                    SerializeObject(property.PropertyType, value, ref writer);
                }
            }
        }
    }

    /// <summary>
    /// Serializes all basic value types, string and enum.
    /// </summary>
    /// <param name="value">valuue to serialize.</param>
    /// <param name="writer">The BinaryWriter to write the data.</param>
    private void SerializeValueType(object value, ref BinaryWriter writer)
    {
        if (value is int iv)
        {
            writer.Write7BitEncodedInt(iv);
        }
        else if (value is long lv)
        {
            writer.Write7BitEncodedInt64(lv);
        }
        else if (value is short sh)
        {
            writer.Write(sh);
        }
        else if (value is byte bt)
        {
            writer.Write(bt);
        }
        else if (value is bool b)
        {
            writer.Write(b);
        }
        else if (value is float f)
        {
            writer.Write(f);
        }
        else if (value is double d)
        {
            writer.Write(d);
        }
        else if (value is string s)
        {
            writer.Write(s);
        }
        else if (value is Enum e)
        {
            var type = e.GetTypeCode();
            switch (type)
            {
                case TypeCode.Int16:
                    writer.Write((short)Convert.ChangeType(value, typeof(short)));
                    break;
                case TypeCode.Int32:
                    writer.Write((int)Convert.ChangeType(value, typeof(int)));
                    break;
                case TypeCode.Int64:
                    writer.Write((int)Convert.ChangeType(value, typeof(long)));
                    break;
                case TypeCode.Byte:
                    writer.Write((byte)Convert.ChangeType(value, typeof(byte)));
                    break;
                case TypeCode.Boolean:
                    writer.Write((bool)Convert.ChangeType(value, typeof(bool)));
                    break;
                case TypeCode.SByte:
                    writer.Write((sbyte)Convert.ChangeType(value, typeof(sbyte)));
                    break;
                case TypeCode.UInt16:
                    writer.Write((ushort)Convert.ChangeType(value, typeof(ushort)));
                    break;
                case TypeCode.UInt32:
                    writer.Write((uint)Convert.ChangeType(value, typeof(uint)));
                    break;
                case TypeCode.UInt64:
                    writer.Write((uint)Convert.ChangeType(value, typeof(ulong)));
                    break;
            }
        }
    }

    /// <summary>
    /// Serializes all collections and arrays.
    /// </summary>
    /// <param name="value">Collection/array value.</param>
    /// <param name="writer">Binary Writer.</param>
    private void SerializeArray(object value, ref BinaryWriter writer)
    {
        if (value is IEnumerable enumerable)
        {
            writer.Write(enumerable.Cast<object>().Count());
            foreach (var item in enumerable)
            {
                var itemType = item.GetType();
                if (itemType.IsValueTypeOrString())
                {
                    if (item is null)
                    {
                        writer.Write((byte)0);
                    }
                    else
                    {
                        SerializeValueType(item, ref writer);
                    }
                }
                else
                {
                    if (item is null)
                    {
                        writer.Write((byte)0);
                    }
                    else
                    {
                        SerializeObject(itemType, item, ref writer);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Deserializes an object from a binary reader into the specified type.
    /// </summary>
    /// <param name="type">Type to deserialize into.</param>
    /// <param name="reader">Binary Reader.</param>
    /// <returns>Deserialized object.</returns>
    private object DeserializeObject(Type type, ref BinaryReader reader)
    {
        object target = Activator.CreateInstance(type)!;

        var properties = type.GetProperties();
        var orderedProperties = properties
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.GetCustomAttributes(typeof(OrderAttribute), true).Length != 0)
            .OrderBy(p => ((OrderAttribute)p.GetCustomAttributes(typeof(OrderAttribute), true)!.FirstOrDefault()!)!.Order);

        foreach (var property in orderedProperties)
        {
            if (property.PropertyType.IsValueTypeOrString())
            {
                DeserializeValueType(reader, property, ref target);
            }
            else if (property.PropertyType.IsCollection())
            {
                DeserializeArray(reader, property, target);
            }
            else
            {
                var item = DeserializeObject(property.PropertyType, ref reader);
                property.SetValue(target, item);
            }
        }

        return target;
    }

    /// <summary>
    /// Deserializes all basic value types, string and enum.
    /// </summary>
    /// <param name="reader">Binary Reader to read.</param>
    /// <param name="property">Property to deserialze into.</param>
    /// <param name="target">Target of the property.</param>
    private void DeserializeValueType(BinaryReader reader, PropertyInfo property, ref object target)
    {
        if (property.PropertyType == typeof(int))
        {
            property.SetValue(target, reader.Read7BitEncodedInt());
        }
        else if (property.PropertyType == typeof(long))
        {
            property.SetValue(target, reader.Read7BitEncodedInt64());
        }
        else if (property.PropertyType == typeof(short))
        {
            property.SetValue(target, reader.ReadInt16());
        }
        else if (property.PropertyType == typeof(byte))
        {
            property.SetValue(target, reader.ReadByte());
        }
        else if (property.PropertyType == typeof(bool))
        {
            property.SetValue(target, reader.ReadBoolean());
        }
        else if (property.PropertyType == typeof(float))
        {
            property.SetValue(target, reader.ReadSingle());
        }
        else if (property.PropertyType == typeof(double))
        {
            property.SetValue(target, reader.ReadDouble());
        }
        else if (property.PropertyType == typeof(string))
        {
            property.SetValue(target, reader.ReadString());
        }
        else if (property.PropertyType.IsEnum)
        {
            var type = Enum.GetUnderlyingType(property.PropertyType);
            object? value = default;
            if (type == typeof(short))
            {
                value = reader.ReadInt16();
            }
            else if (type == typeof(int))
            {
                value = reader.ReadInt32();
            }
            else if (type == typeof(long))
            {
                value = reader.ReadInt64();
            }
            else if (type == typeof(byte))
            {
                value = reader.ReadByte();
            }
            else if (type == typeof(bool))
            {
                value = reader.ReadBoolean();
            }
            else if (type == typeof(sbyte))
            {
                value = reader.ReadSByte();
            }
            else if (type == typeof(ushort))
            {
                value = reader.ReadUInt16();
            }
            else if (type == typeof(uint))
            {
                value = reader.ReadUInt32();
            }
            else if (type == typeof(ulong))
            {
                value = reader.ReadUInt64();
            }

            property.SetValue(target, Enum.ToObject(property.PropertyType, value!));
        }
    }

    /// <summary>
    /// Deserializes all basic value types, string and enum.
    /// </summary>
    /// <param name="reader">Binary Reader to read.</param>
    /// <param name="property">Field to deserialze into.</param>
    /// <param name="target">Target of the property.</param>
    private void DeserializeValueType(BinaryReader reader, FieldInfo property, ref object target)
    {
        if (property.FieldType == typeof(int))
        {
            property.SetValue(target, reader.Read7BitEncodedInt());
        }
        else if (property.FieldType == typeof(long))
        {
            property.SetValue(target, reader.Read7BitEncodedInt64());
        }
        else if (property.FieldType == typeof(short))
        {
            property.SetValue(target, reader.ReadInt16());
        }
        else if (property.FieldType == typeof(byte))
        {
            property.SetValue(target, reader.ReadByte());
        }
        else if (property.FieldType == typeof(bool))
        {
            property.SetValue(target, reader.ReadBoolean());
        }
        else if (property.FieldType == typeof(float))
        {
            property.SetValue(target, reader.ReadSingle());
        }
        else if (property.FieldType == typeof(double))
        {
            property.SetValue(target, reader.ReadDouble());
        }
        else if (property.FieldType == typeof(string))
        {
            property.SetValue(target, reader.ReadString());
        }
        else if (property.FieldType.IsEnum)
        {
            var type = Enum.GetUnderlyingType(property.FieldType);
            object? value = default;
            if (type == typeof(short))
            {
                value = reader.ReadInt16();
            }
            else if (type == typeof(int))
            {
                value = reader.ReadInt32();
            }
            else if (type == typeof(long))
            {
                value = reader.ReadInt64();
            }
            else if (type == typeof(byte))
            {
                value = reader.ReadByte();
            }
            else if (type == typeof(bool))
            {
                value = reader.ReadBoolean();
            }
            else if (type == typeof(sbyte))
            {
                value = reader.ReadSByte();
            }
            else if (type == typeof(ushort))
            {
                value = reader.ReadUInt16();
            }
            else if (type == typeof(uint))
            {
                value = reader.ReadUInt32();
            }
            else if (type == typeof(ulong))
            {
                value = reader.ReadUInt64();
            }

            property.SetValue(target, Enum.ToObject(property.FieldType, value!));
        }
    }

    /// <summary>
    /// Deserializes array and collections.
    /// </summary>
    /// <param name="reader">Binary Reader to read.</param>
    /// <param name="property">Property to deserialize into.</param>
    /// <param name="target">Target of the property.</param>
    private void DeserializeArray(BinaryReader reader, PropertyInfo property, object target)
    {
        if (property.PropertyType.IsArray)
        {
            var length = reader.ReadInt32();
            var arrayType = property.PropertyType.GetElementType()!;
            var array = Array.CreateInstance(arrayType, length);
            for (int i = 0; i < length; i++)
            {
                dynamic val;
                if (arrayType.IsValueType)
                {
                    val = Activator.CreateInstance(arrayType)!;
                    var pi = arrayType.GetField("m_value", BindingFlags.NonPublic | BindingFlags.Instance);
                    DeserializeValueType(reader, pi!, ref val);
                }
                else if (arrayType == typeof(string))
                {
                    val = string.Empty;
                    val = reader.ReadString();
                }
                else
                {
                    val = DeserializeObject(arrayType, ref reader);
                }

                array.SetValue(val, i);
            }

            property.SetValue(target, array);
        }
        else if (property.PropertyType.IsAssignableTo(typeof(IEnumerable)))
        {
            var length = reader.ReadInt32();
            var arrayType = property.PropertyType.GetGenericArguments()[0];
            var genericEnumerable = typeof(IEnumerable<>).MakeGenericType(arrayType);
            var list = (IEnumerable)Activator.CreateInstance(typeof(List<>).MakeGenericType(arrayType))!;
            for (int i = 0; i < length; i++)
            {
                dynamic val;
                if (arrayType.IsValueType)
                {
                    val = Activator.CreateInstance(arrayType)!;
                    var pi = arrayType.GetField("m_value", BindingFlags.NonPublic | BindingFlags.Instance);
                    DeserializeValueType(reader, pi!, ref val);
                }
                else if (arrayType == typeof(string))
                {
                    val = string.Empty;
                    val = reader.ReadString();
                }
                else
                {
                    val = DeserializeObject(arrayType, ref reader);
                }

                ((IList)list).Add(val);
            }

            property.SetValue(target, list);
        }
    }
}

/// <summary>
/// Extensions for serialization.
/// </summary>
internal static class SerializationExtensions
{
    /// <summary>
    /// Checks whether the type is a collection or an array.
    /// </summary>
    /// <param name="pi">Type.</param>
    /// <returns>A value indicating whether the type is a collection or not.</returns>
    public static bool IsCollection(this Type pi)
    {
        return pi.IsArray || typeof(IEnumerable).IsAssignableFrom(pi) || typeof(ICollection).IsAssignableFrom(pi);
    }

    /// <summary>
    /// Checks whether the type is a value type or a string.
    /// </summary>
    /// <param name="pi">Type.</param>
    /// <returns>A value indicating whether the type is value type or a string.</returns>
    public static bool IsValueTypeOrString(this Type pi)
    {
        return pi.IsValueType || pi == typeof(string);
    }
}
