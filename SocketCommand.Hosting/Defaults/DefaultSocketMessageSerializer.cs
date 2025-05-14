using SocketCommand.Abstractions.Attributes;
using SocketCommand.Abstractions.Interfaces;
using System.Collections;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SocketCommand.Hosting.Defaults;

public sealed class DefaultSocketMessageSerializer : ISocketMessageSerializer
{
    public object? Deserialize(byte[] data, Type type)
    {
        var attr = type.GetCustomAttributes(typeof(SocketMessageAttribute), true).FirstOrDefault() as SocketMessageAttribute;

        if (attr is null)
            throw new ArgumentException(nameof(data), $"Type {type.Name} is not decorated as serializable. Please add the attribute [{nameof(SocketMessageAttribute)}] to your type and [{nameof(OrderAttribute)}] to the properties to serialize.");

        using var memstr = new MemoryStream(data);
        var reader = new BinaryReader(memstr);
        var deserialized = DeserializeObject(type, ref reader);

        reader.Close();
        reader.Dispose();
        return deserialized;
    }

    public byte[] Serialize<T>(T data)
    {
        return Serialize((object)data);
    }

    public byte[] Serialize(object data)
    {
        var type = data.GetType();
        var attr = type.GetCustomAttributes(typeof(SocketMessageAttribute), true).FirstOrDefault() as SocketMessageAttribute;

        if (attr is null)
            throw new ArgumentException(nameof(data), $"Type {type.Name} is not decorated as serializable. Please add the attribute [{nameof(SocketMessageAttribute)}] to your type and [{nameof(OrderAttribute)}] to the properties to serialize.");

        using var memstr = new MemoryStream();
        var writer = new BinaryWriter(memstr);
        SerializeObject(type, data, ref writer);

        writer.Flush();
        writer.Dispose();
        return memstr.ToArray();
    }

    private void SerializeObject(Type type, object o, ref BinaryWriter writer)
    {
        var properties = type.GetProperties();
        var orderedProperties = properties
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.GetCustomAttributes(typeof(OrderAttribute), true).Length != 0)
            .OrderBy(p => ((OrderAttribute)p.GetCustomAttributes(typeof(OrderAttribute), true)!.FirstOrDefault())!.Order);

        foreach (var property in orderedProperties)
        {
            var value = property.GetValue(o);
            if (property.PropertyType.IsValueTypeOrString())
            {
                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (value is null)
                        writer.Write((byte)0);
                    else
                        SerializeValueType(value, ref writer);
                }
                else
                    SerializeValueType(value, ref writer);
            }
            else if (property.PropertyType.IsCollection())
            {
                SerializeArray(value, ref writer);
            }
            else
            {
                if (value is null)
                    writer.Write((byte)0);
                else
                    SerializeObject(property.PropertyType, value, ref writer);
            }
        }
    }

    private void SerializeValueType(object value, ref BinaryWriter writer)
    {
        if (value is int iv)
            writer.Write7BitEncodedInt(iv);
        else if (value is long lv)
            writer.Write7BitEncodedInt64(lv);
        else if (value is short sh)
            writer.Write(sh);
        else if (value is byte bt)
            writer.Write(bt);
        else if (value is bool b)
            writer.Write(b);
        else if (value is float f)
            writer.Write(f);
        else if (value is double d)
            writer.Write(d);
        else if (value is string s)
            writer.Write(s);
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
                        writer.Write((byte)0);
                    else
                        SerializeValueType(item, ref writer);
                }
                else
                {
                    if (item is null)
                        writer.Write((byte)0);
                    else
                        SerializeObject(itemType, item, ref writer);
                }
            }
        }
    }

    private object DeserializeObject(Type type, ref BinaryReader reader)
    {
        object target = Activator.CreateInstance(type);

        var properties = type.GetProperties();
        var orderedProperties = properties
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.GetCustomAttributes(typeof(OrderAttribute), true).Length != 0)
            .OrderBy(p => ((OrderAttribute)p.GetCustomAttributes(typeof(OrderAttribute), true)!.FirstOrDefault())!.Order);


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

    private void DeserializeValueType(BinaryReader reader, PropertyInfo property, ref object target)
    {
        if (property.PropertyType == typeof(int))
            property.SetValue(target, reader.Read7BitEncodedInt());
        else if (property.PropertyType == typeof(long))
            property.SetValue(target, reader.Read7BitEncodedInt64());
        else if (property.PropertyType == typeof(short))
            property.SetValue(target, reader.ReadInt16());
        else if (property.PropertyType == typeof(byte))
            property.SetValue(target, reader.ReadByte());
        else if (property.PropertyType == typeof(bool))
            property.SetValue(target, reader.ReadBoolean());
        else if (property.PropertyType == typeof(float))
            property.SetValue(target, reader.ReadSingle());
        else if (property.PropertyType == typeof(double))
            property.SetValue(target, reader.ReadDouble());
        else if (property.PropertyType == typeof(string))
            property.SetValue(target, reader.ReadString());
        else if (property.PropertyType.IsEnum)
        {
            var type = Enum.GetUnderlyingType(property.PropertyType);
            object value = null;
            if (type == typeof(short))
                value = reader.ReadInt16();
            else if (type == typeof(int))
                value = reader.ReadInt32();
            else if (type == typeof(long))
                value = reader.ReadInt64();
            else if (type == typeof(byte))
                value = reader.ReadByte();
            else if (type == typeof(bool))
                value = reader.ReadBoolean();
            else if (type == typeof(sbyte))
                value = reader.ReadSByte();
            else if (type == typeof(ushort))
                value = reader.ReadUInt16();
            else if (type == typeof(uint))
                value = reader.ReadUInt32();
            else if (type == typeof(ulong))
                value = reader.ReadUInt64();

            property.SetValue(target, Enum.ToObject(property.PropertyType, value));
        }
    }

    private void DeserializeValueType(BinaryReader reader, FieldInfo property, ref object target)
    {
        if (property.FieldType == typeof(int))
            property.SetValue(target, reader.Read7BitEncodedInt());
        else if (property.FieldType == typeof(long))
            property.SetValue(target, reader.Read7BitEncodedInt64());
        else if (property.FieldType == typeof(short))
            property.SetValue(target, reader.ReadInt16());
        else if (property.FieldType == typeof(byte))
            property.SetValue(target, reader.ReadByte());
        else if (property.FieldType == typeof(bool))
            property.SetValue(target, reader.ReadBoolean());
        else if (property.FieldType == typeof(float))
            property.SetValue(target, reader.ReadSingle());
        else if (property.FieldType == typeof(double))
            property.SetValue(target, reader.ReadDouble());
        else if (property.FieldType == typeof(string))
            property.SetValue(target, reader.ReadString());
        else if (property.FieldType.IsEnum)
        {
            var type = Enum.GetUnderlyingType(property.FieldType);
            object value = null;
            if (type == typeof(short))
                value = reader.ReadInt16();
            else if (type == typeof(int))
                value = reader.ReadInt32();
            else if (type == typeof(long))
                value = reader.ReadInt64();
            else if (type == typeof(byte))
                value = reader.ReadByte();
            else if (type == typeof(bool))
                value = reader.ReadBoolean();
            else if (type == typeof(sbyte))
                value = reader.ReadSByte();
            else if (type == typeof(ushort))
                value = reader.ReadUInt16();
            else if (type == typeof(uint))
                value = reader.ReadUInt32();
            else if (type == typeof(ulong))
                value = reader.ReadUInt64();

            property.SetValue(target, Enum.ToObject(property.FieldType, value));
        }
    }

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
                    val = Activator.CreateInstance(arrayType);
                    var pi = arrayType.GetField("m_value", BindingFlags.NonPublic | BindingFlags.Instance);
                    DeserializeValueType(reader, pi, ref val);
                }
                else if (arrayType ==  typeof(string))
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
                    val = Activator.CreateInstance(arrayType);
                    var pi = arrayType.GetField("m_value", BindingFlags.NonPublic | BindingFlags.Instance);
                    DeserializeValueType(reader, pi, ref val);
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
static class SerializationExtensions
{
    public static bool IsCollection(this Type pi)
    {
        return pi.IsArray || typeof(IEnumerable).IsAssignableFrom(pi) || typeof(ICollection).IsAssignableFrom(pi);
    }

    public static bool IsValueTypeOrString(this Type pi)
    {
        return pi.IsValueType || pi == typeof(string);
    }

    public static object GetDefaultValue(this Type type)
    {
        if (type == null) throw new ArgumentNullException("type");

        Expression<Func<object>> e = Expression.Lambda<Func<object>>(
            Expression.Convert(
                Expression.Default(type), typeof(object)
            )
        );

        return e.Compile()();
    }
}