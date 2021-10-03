using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Discreet.Utilities
{
    /**
     * <summary>
     * This attribute is used by the JsonSerializer to recognize complete Jsonable objects.
     * </summary>
     */
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class JsonObject: Attribute
    {
        public string name;

        public JsonObject(string name)
        {
            this.name = name;
        }
    }

    /**
     * <summary>
     * Specifies type information used by the JsonSerializer.
     * </summary>
     */
    public enum JsonType
    {
        None,
        Struct,
        Integer,
        UnsignedInteger,
        Long,
        UnsignedLong,
        Short,
        UnsignedShort,
        Decimal,
        String,
        Hex,
        Byte,
        Array,
    }

    /**
     * <summary>
     * This attribute is used by the JsonSerializer.
     * the name field is  the json key.
     * JsonTypes are used for subtype (for collections) and main type.
     * </summary>
     */
    [AttributeUsage(AttributeTargets.Field)]
    public class JsonElement: Attribute
    {
        public string name;
        public JsonType type;
        public JsonType subtype; //valid only for Array type
        
        public JsonElement(string name, JsonType type, JsonType subtype = JsonType.None)
        {
            this.name = name;
            this.type = type;

            if (subtype != JsonType.None && type != JsonType.Array)
            {
                throw new Exception($"Discreet.Utilities.JsonElement: does not accept array subtype if type is not array; got type of {type}");
            }
            this.subtype = subtype;
        }

        public JsonElement(string name)
        {
            /* type will be inferred */
            this.name = name;
            this.type = JsonType.None;
            this.subtype = JsonType.None;
        }
    }

    /**
     * <summary>
     * This attribute is used by JsonSerializer to ignore serialization of a field.
     * </summary>
     */
    [AttributeUsage(AttributeTargets.Field)]
    public class JsonIgnore : Attribute
    {

    }

    public static class JsonSerializer
    {
        /**
        * <summary>
        * Attempts to serialize the object into the specified type.
        * If attributes are not used, the object is serialized according to type information.
        * </summary>
        */
        public static string Serialize<T>(object o)
        {
            if (o == null) return "";

            if (o.GetType() != typeof(T)) throw new Exception($"Discreet.Utilities.JsonSerializer.Serialize<{typeof(T)}>: types do not agree!");

            var objAttr = Attribute.GetCustomAttribute(o.GetType(), typeof(JsonObject));

            if (objAttr == null)
            {
                throw new Exception($"Discreet.Utilities.JsonSerializer.Serialize<{typeof(T)}>: object of type {o.GetType()} does not specify a JsonObject");
            }

            string rv = "";

            var fields = o.GetType().GetFields();

            if (fields == null || fields.Length == 0) return "{}";

            for (int i = 0; i < fields.Length; i++)
            {
                var attr = Attribute.GetCustomAttribute(fields[i].FieldType, typeof(JsonElement));

                if (attr == null) rv += SerializeField(fields[i], o);
            }

            return null;
        }

        public static string Serialize(object o)
        {
            if (o == null) return "";

            string rv = "";

            var fields = o.GetType().GetFields();

            if (fields == null || fields.Length == 0) return SerializeFieldValue(o.GetType(), o);

            rv += "{";

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsStatic) continue;

                var attr = Attribute.GetCustomAttribute(fields[i].FieldType, typeof(JsonElement));

                var ignore = Attribute.GetCustomAttribute(fields[i], typeof(JsonIgnore));

                if (ignore != null) continue;

                if (attr == null) rv += SerializeField(fields[i], o);

                if (i < fields.Length - 1)
                {
                    rv += ",";
                }
            }

            return (rv.EndsWith(",") ? rv.Substring(0, rv.Length - 1) : rv) + "}";
        }

        private static string SerializeField(FieldInfo field, object obj)
        {
            //Console.WriteLine("Serializing field " + field.Name + "...");

            string rv = $"\"{field.Name}\":";

            dynamic value = Convert.ChangeType(field.GetValue(obj), field.FieldType);

            return rv + SerializeFieldValue(field.FieldType, value);
        }

        private static string SerializeFieldValue(Type type, dynamic value)
        {
            //Console.WriteLine($"Serializing value {value}...");

            string valueString = "";

            if (!type.IsValueType && value == null) valueString = "null";

            else if (type == typeof(string)) valueString = $"\"{value}\"";

            else if (type == typeof(byte[]))
            {
                valueString = $"\"{Coin.Printable.Hexify(value)}\"";
            }

            else if (type == typeof(Cipher.Key))
            {
                valueString = $"\"{Coin.Printable.Hexify(value.bytes)}\"";
            }

            else if (type.GetInterface(typeof(Cipher.Hash).Name) != null)
            {
                valueString = $"\"{Coin.Printable.Hexify(value.GetBytes())}\"";
            }

            else if (type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(decimal) ||
                type == typeof(double) ||
                type == typeof(float) ||
                type == typeof(byte) ||
                type == typeof(sbyte))
                valueString = $"{value}";

            else if (type.IsArray)
            {
                valueString = SerializeArray(type, value);
            }

            else if (type.GetInterfaces().Any(x=>x.Name.StartsWith("IEnumerable")))
            {
                valueString = SerializeArray(type, value);
            }

            else if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
            {
                valueString = Serialize(value);
            }

            return valueString;
        }

        private static string SerializeArray(Type type, dynamic value)
        {
            List<string> valueStrings = new List<string>();

            foreach(var val in value)
            {
                Type subtype = (type.IsArray) ? type.GetElementType() : type.GetGenericArguments().Single();
                valueStrings.Add(SerializeFieldValue(subtype, val));
            }

            return $"[{String.Join(',', valueStrings)}]";
        }


        public static T Deserialize<T>(string json)
        {
            throw new NotImplementedException("To be implemented.");
        }
    }
}
