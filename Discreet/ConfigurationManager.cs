using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Discreet
{
    public class ConfigurationManager : IDisposable
    {
        /* ConfigurationManager Class
         * - Load a .json file based on the path
         * - Retrieve a generic value from the json file
         */
        private JsonDocument _document;

        public ConfigurationManager(string configurationFilePath)
        {
            string configContent = File.ReadAllText(configurationFilePath);
            _document = JsonDocument.Parse(configContent);
        }

        /// <summary>
        /// Get a generic value based on a key. Supports nested objects using the delimiter ':'
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetValue<T>(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key param were not specified");

            // Get the desired json object
            var nestedKeys = key.Split(":");
            JsonElement temp = _document.RootElement;
            for (int i = 0; i < nestedKeys.Length - 1; i++)     // -1, to ensure we dont used the final key
            {
                temp = temp.GetProperty(nestedKeys[i]);
            }

            var valueKey = nestedKeys.Last();

            // Retrieve the value
            object value = null;
            if (typeof(T) == typeof(string))            value = temp.GetProperty(valueKey).GetString();             // e.g. "Hello world"

            if (typeof(T) == typeof(short))             value = temp.GetProperty(valueKey).GetInt16();              // e.g. -1
            if (typeof(T) == typeof(ushort))            value = temp.GetProperty(valueKey).GetUInt16();             // e.g. 1
            if (typeof(T) == typeof(int))               value = temp.GetProperty(valueKey).GetInt32();              // e.g. -1
            if (typeof(T) == typeof(uint))              value = temp.GetProperty(valueKey).GetUInt32();             // e.g. 1
            if (typeof(T) == typeof(long))              value = temp.GetProperty(valueKey).GetInt64();              // e.g. -1
            if (typeof(T) == typeof(ulong))             value = temp.GetProperty(valueKey).GetUInt64();             // e.g. 1
            if (typeof(T) == typeof(decimal))           value = temp.GetProperty(valueKey).GetDecimal();            // e.g. 1.0
            if (typeof(T) == typeof(double))            value = temp.GetProperty(valueKey).GetDouble();             // e.g. 1.0
            if (typeof(T) == typeof(float))             value = temp.GetProperty(valueKey).GetSingle();             // e.g. 1.0

            if (typeof(T) == typeof(bool))              value = temp.GetProperty(valueKey).GetBoolean();            // e.g. true / false
            if (typeof(T) == typeof(byte))              value = temp.GetProperty(valueKey).GetByte();               // e.g. 1
            if (typeof(T) == typeof(sbyte))             value = temp.GetProperty(valueKey).GetSByte();              // e.g. 1

            if (typeof(T) == typeof(DateTime))          value = temp.GetProperty(valueKey).GetDateTime();           // e.g. "2020-08-01T12:50:10"
            if (typeof(T) == typeof(DateTimeOffset))    value = temp.GetProperty(valueKey).GetDateTimeOffset();     // e.g. "2020-08-01T12:50:10"
            if (typeof(T) == typeof(Guid))              value = temp.GetProperty(valueKey).GetGuid();               // e.g. "23c82180-f073-4bc6-82b2-8058fb9ada05"

            return (T)Convert.ChangeType(value, typeof(T));

            throw new ArgumentException("Not supported", nameof(T));
        }
        public void Dispose()
        {
            _document?.Dispose();
        }
    }
}
