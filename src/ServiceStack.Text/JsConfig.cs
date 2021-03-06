using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text
{
    public static class JsConfig
    {
        static JsConfig()
        {
            //In-built default serialization, to Deserialize Color struct do:
            //JsConfig<System.Drawing.Color>.SerializeFn = c => c.ToString().Replace("Color ", "").Replace("[", "").Replace("]", "");
            //JsConfig<System.Drawing.Color>.DeSerializeFn = System.Drawing.Color.FromName;
        }

        [ThreadStatic]
        public static bool ConvertObjectTypesIntoStringDictionary = false;

        [ThreadStatic]
        public static bool IncludeNullValues = false;

        [ThreadStatic]
        public static bool ExcludeTypeInfo = false;

        /// <summary>
        /// <see langword="true"/> if the <see cref="ITypeSerializer"/> is configured
        /// to take advantage of <see cref="CLSCompliantAttribute"/> specification,
        /// to support user-friendly serialized formats, ie emitting camelCasing for JSON
        /// and parsing member names and enum values in a case-insensitive manner.
        /// </summary>
        public static bool EmitCamelCaseNames
        {
            // obeying the use of ThreadStatic, but allowing for setting JsConfig once as is the normal case
            get
            {
                return tsEmitCamelCaseNames ?? sEmitCamelCaseNames ?? false;
            }
            set
            {
                if (!tsEmitCamelCaseNames.HasValue) tsEmitCamelCaseNames = value;
                if (!sEmitCamelCaseNames.HasValue) sEmitCamelCaseNames = value;
            }
        }

        [ThreadStatic]
        private static bool? tsEmitCamelCaseNames;
        private static bool? sEmitCamelCaseNames;

        internal static HashSet<Type> HasSerializeFn = new HashSet<Type>();

        public static void Reset()
        {
            ConvertObjectTypesIntoStringDictionary = IncludeNullValues = ExcludeTypeInfo = false;
            tsEmitCamelCaseNames = sEmitCamelCaseNames = null;
            HasSerializeFn = new HashSet<Type>();
        }

#if SILVERLIGHT || MONOTOUCH
        /// <summary>
        /// Provide hint to MonoTouch AOT compiler to pre-compile generic classes for all your DTOs.
        /// Just needs to be called once in a static constructor.
        /// </summary>
        public static void InitForAot() { }

        public static void RegisterForAot()
        {
            JsonAotConfig.Register<Poco>();

            RegisterElement<Poco, string>();

            RegisterElement<Poco, bool>();
            RegisterElement<Poco, char>();
            RegisterElement<Poco, byte>();
            RegisterElement<Poco, sbyte>();
            RegisterElement<Poco, short>();
            RegisterElement<Poco, ushort>();
            RegisterElement<Poco, int>();
            RegisterElement<Poco, uint>();
            RegisterElement<Poco, long>();
            RegisterElement<Poco, ulong>();
            RegisterElement<Poco, float>();
            RegisterElement<Poco, double>();
            RegisterElement<Poco, decimal>();
            RegisterElement<Poco, Guid>();
            RegisterElement<Poco, DateTime>();
            RegisterElement<Poco, TimeSpan>();

            RegisterElement<Poco, bool?>();
            RegisterElement<Poco, char?>();
            RegisterElement<Poco, byte?>();
            RegisterElement<Poco, sbyte?>();
            RegisterElement<Poco, short?>();
            RegisterElement<Poco, ushort?>();
            RegisterElement<Poco, int?>();
            RegisterElement<Poco, uint?>();
            RegisterElement<Poco, long?>();
            RegisterElement<Poco, ulong?>();
            RegisterElement<Poco, float?>();
            RegisterElement<Poco, double?>();
            RegisterElement<Poco, decimal?>();
            RegisterElement<Poco, Guid?>();
            RegisterElement<Poco, DateTime?>();
            RegisterElement<Poco, TimeSpan?>();

            RegisterQueryStringWriter();
            RegisterCsvSerializer();
        }

        static void RegisterQueryStringWriter()
        {
            var i = 0;
            if (QueryStringWriter<Poco>.WriteFn() != null) i++;
        }

        static void RegisterCsvSerializer()
        {
            CsvSerializer<Poco>.WriteFn();
            CsvSerializer<Poco>.WriteObject(null, null);
            CsvWriter<Poco>.WriteObject(null, null);
            CsvWriter<Poco>.WriteObjectRow(null, null);
        }

        public static void RegisterElement<T, TElement>()
        {
            JsonAotConfig.RegisterElement<T, TElement>();
        }
#endif

    }

#if SILVERLIGHT || MONOTOUCH
    internal class Poco
    {
        public string Dummy { get; set; }
    }

    internal class JsonAotConfig
    {
        static JsReader<JsonTypeSerializer> reader;
        static JsonTypeSerializer serializer;

        static JsonAotConfig()
        {
            serializer = new JsonTypeSerializer();
            reader = new JsReader<JsonTypeSerializer>();
        }

        public static ParseStringDelegate GetParseFn(Type type)
        {
            var parseFn = JsonTypeSerializer.Instance.GetParseFn(type);
            return parseFn;
        }

        internal static ParseStringDelegate RegisterBuiltin<T>()
        {
            var i = 0;
            if (reader.GetParseFn<T>() != null) i++;
            if (JsonReader<T>.GetParseFn() != null) i++;
            if (JsonReader<T>.Parse(null) != null) i++;
            if (JsonWriter<T>.WriteFn() != null) i++;

            return serializer.GetParseFn<T>();
        }

        public static void Register<T>()
        {
            var i = 0;
            var serializer = JsonTypeSerializer.Instance;
            if (new List<T>() != null) i++;
            if (new T[0] != null) i++;
            if (serializer.GetParseFn<T>() != null) i++;
            if (DeserializeArray<T[], JsonTypeSerializer>.Parse != null) i++;

            JsConfig<T>.ExcludeTypeInfo = false;
            //JsConfig<T>.SerializeFn = arg => "";
            //JsConfig<T>.DeSerializeFn = arg => default(T);

            DeserializeArrayWithElements<T, JsonTypeSerializer>.ParseGenericArray(null, null);
            DeserializeCollection<JsonTypeSerializer>.ParseCollection<T>(null, null, null);
            DeserializeListWithElements<T, JsonTypeSerializer>.ParseGenericList(null, null, null);

            SpecializedQueueElements<T>.ConvertToQueue(null);
            SpecializedQueueElements<T>.ConvertToStack(null);

            WriteListsOfElements<T, JsonTypeSerializer>.WriteList(null, null);
            WriteListsOfElements<T, JsonTypeSerializer>.WriteIList(null, null);
            WriteListsOfElements<T, JsonTypeSerializer>.WriteEnumerable(null, null);
            WriteListsOfElements<T, JsonTypeSerializer>.WriteListValueType(null, null);
            WriteListsOfElements<T, JsonTypeSerializer>.WriteIListValueType(null, null);

            JsonReader<T>.Parse(null);
            JsonWriter<T>.WriteFn();

            TranslateListWithElements<T>.LateBoundTranslateToGenericICollection(null, null);
            TranslateListWithConvertibleElements<T, T>.LateBoundTranslateToGenericICollection(null, null);

            QueryStringWriter<T>.WriteObject(null, null);
        }

        public static void RegisterElement<T, TElement>()
        {
            RegisterBuiltin<TElement>();
            DeserializeDictionary<JsonTypeSerializer>.ParseDictionary<T, TElement>(null, null, null, null);
            DeserializeDictionary<JsonTypeSerializer>.ParseDictionary<TElement, T>(null, null, null, null);

            ToStringDictionaryMethods<T, TElement, JsonTypeSerializer>.WriteIDictionary(null, null, null, null);
            ToStringDictionaryMethods<TElement, T, JsonTypeSerializer>.WriteIDictionary(null, null, null, null);

            TranslateListWithElements<TElement>.LateBoundTranslateToGenericICollection(null, typeof(List<TElement>));
            TranslateListWithConvertibleElements<TElement, TElement>.LateBoundTranslateToGenericICollection(null, typeof(List<TElement>));
        }
    }
#endif

    public class JsConfig<T> //where T : struct
    {
        /// <summary>
        /// Never emit type info for this type
        /// </summary>
        public static bool ExcludeTypeInfo = false;

        /// <summary>
        /// <see langword="true"/> if the <see cref="ITypeSerializer"/> is configured
        /// to take advantage of <see cref="CLSCompliantAttribute"/> specification,
        /// to support user-friendly serialized formats, ie emitting camelCasing for JSON
        /// and parsing member names and enum values in a case-insensitive manner.
        /// </summary>
        public static bool EmitCamelCaseNames = false;

        /// <summary>
        /// Define custom serialization fn for BCL Structs
        /// </summary>
        private static Func<T, string> serializeFn;
        public static Func<T, string> SerializeFn
        {
            get { return serializeFn; }
            set
            {
                serializeFn = value;
                if (value != null)
                    JsConfig.HasSerializeFn.Add(typeof(T));
                else
                    JsConfig.HasSerializeFn.Remove(typeof(T));
            }
        }

        /// <summary>
        /// Define custom deserialization fn for BCL Structs
        /// </summary>
        public static Func<string, T> DeSerializeFn;

        /// <summary>
        /// Exclude specific properties of this type from being serialized
        /// </summary>
        public static string[] ExcludePropertyNames;

        public static void WriteFn<TSerializer>(TextWriter writer, object obj)
        {
            var serializer = JsWriter.GetTypeSerializer<TSerializer>();
            serializer.WriteRawString(writer, SerializeFn((T)obj));
        }

        public static object ParseFn(string str)
        {
            return DeSerializeFn(str);
        }
    }

}

