using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{
    public static class SqlPersistenceConfig
    {
        static Lazy<Newtonsoft.Json.JsonSerializer> lazySerializer;
        static Lazy<Func<StringBuilder, JsonWriter>> lazyWriterCreator;
        static Lazy<Func<TextReader, JsonReader>> lazyReaderCreator;

        static SqlPersistenceConfig()
        {
            lazySerializer = new Lazy<Newtonsoft.Json.JsonSerializer>(Newtonsoft.Json.JsonSerializer.Create);
            lazyReaderCreator = new Lazy<Func<TextReader, JsonReader>>(() => (reader => new JsonTextReader(reader)));
            lazyWriterCreator = new Lazy<Func<StringBuilder, JsonWriter>>(() => (builder =>
            {
                var writer = new StringWriter(builder);
                return new JsonTextWriter(writer)
                {
                    Formatting = Formatting.None
                };
            }) );
        }

        public static void WriterCreator(this PersistenceExtensions<SqlPersistence> configuration, Func<StringBuilder, JsonWriter> writerCreator)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.WriterCreator", writerCreator);
        }

        public static void WriterCreator<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, Func<StringBuilder, JsonWriter> writerCreator)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.WriterCreator";
            configuration.GetSettings()
                .Set(key, writerCreator);
        }


        internal static Func<StringBuilder, JsonWriter> GetWriterCreator<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetValue<Func<StringBuilder, JsonWriter>, TStorageType>("WriterCreator", () => lazyWriterCreator.Value);
        }

        public static void ReaderCreator(this PersistenceExtensions<SqlPersistence> configuration, Func<TextReader, JsonReader> readerCreator)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.ReaderCreator", readerCreator);
        }

        public static void ReaderCreator<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, Func<TextReader, JsonReader> readerCreator)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.ReaderCreator";
            configuration.GetSettings()
                .Set(key, readerCreator);
        }


        internal static Func<TextReader, JsonReader> GetReaderCreator<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetValue<Func<TextReader, JsonReader>, TStorageType>("ReaderCreator", () => lazyReaderCreator.Value);
        }


        public static void ConnectionString(this PersistenceExtensions<SqlPersistence> configuration, string connectionString)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.ConnectionString", connectionString);
        }

        public static void ConnectionString<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, string connectionString)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.ConnectionString";
            configuration.GetSettings()
                .Set(key, connectionString);
        }

        internal static string GetConnectionString<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetValue<string, TStorageType>("ConnectionString", () => { throw new Exception("ConnectionString must be defined."); });
        }


        public static void JsonSettings(this PersistenceExtensions<SqlPersistence> configuration, JsonSerializerSettings settings)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.Settings", settings);
        }

        public static void JsonSettings<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, JsonSerializerSettings settings)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.JsonSerializerSettings";
            configuration.GetSettings()
                .Set(key, settings);
        }


        internal static Newtonsoft.Json.JsonSerializer GetJsonSerializer<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            var serializerSettings = settings.GetValue<JsonSerializerSettings, TStorageType>("JsonSerializerSettings", () => null);
            if (serializerSettings == null)
            {
                return lazySerializer.Value;
            }
            return Newtonsoft.Json.JsonSerializer.Create(serializerSettings);
        }


        public static void UseEndpointName(this PersistenceExtensions<SqlPersistence> configuration, bool useEndpointName)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.UseEndpointName", useEndpointName);
        }

        public static void UseEndpointName<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, bool useEndpointName)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.UseEndpointName";
            configuration.GetSettings()
                .Set(key, useEndpointName);
        }

        static bool ShouldUseEndpointName<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetValue<bool, TStorageType>("UseEndpointName", () => true);
        }

        internal static string GetEndpointNamePrefix<T>(this ReadOnlySettings settings) where T : StorageType
        {
            if (settings.ShouldUseEndpointName<T>())
            {
                return $"{settings.EndpointName()}.";
            }
            return "";
        }


        public static void DisableInstaller(this PersistenceExtensions<SqlPersistence> configuration)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.DisableInstaller", true);
        }

        public static void DisableInstaller<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.DisableInstaller";
            configuration.GetSettings()
                .Set(key, true);
        }

        internal static bool GetDisableInstaller<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetValue<bool, TStorageType>("DisableInstaller", () => false);
        }

        internal static void EnableFeature<TStorageType>(this ReadOnlySettings settingsHolder)
            where TStorageType : StorageType
        {
            settingsHolder.Get<EnabledStorageFeatures>().Enable<TStorageType>();
        }

        internal static bool GetFeatureEnabled<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetOrDefault<EnabledStorageFeatures>()?.IsEnabled<TStorageType>() ?? false;
        }

        public static bool ShouldInstall<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            var featureEnabled = settings.GetFeatureEnabled<TStorageType>();
            var disableInstaller = settings.GetDisableInstaller<TStorageType>();
            return
                featureEnabled &&
                !disableInstaller;
        }

        public static void Schema(this PersistenceExtensions<SqlPersistence> configuration, string schema)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.Schema", schema);
        }

        public static void Schema<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, string schema)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.Schema";
            configuration.GetSettings()
                .Set(key, schema);
        }

        internal static string GetSchema<TStorageType>(this ReadOnlySettings settings) where TStorageType : StorageType
        {
            return settings.GetValue<string, TStorageType>("Schema", () => "dbo");
        }

        internal static TValue GetValue<TValue, TStorageType>(this ReadOnlySettings settings, string suffix, Func<TValue> defaultValue)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof (TStorageType).Name}.{suffix}";
            TValue value;
            if (settings.TryGet(key, out value))
            {
                return value;
            }
            if (settings.TryGet($"SqlPersistence.{suffix}", out value))
            {
                return value;
            }
            return defaultValue();
        }
    }
}