using System;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{
    public static partial class SqlPersistenceConfig
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


        public static void ConnectionBuilder(this PersistenceExtensions<SqlPersistence> configuration, Func<Task<DbConnection>> connectionBuilder)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.ConnectionBuilder", connectionBuilder);
        }

        public static void ConnectionBuilder<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, Func<Task<DbConnection>> connectionBuilder)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.ConnectionBuilder";
            configuration.GetSettings()
                .Set(key, connectionBuilder);
        }

        public static void ConnectionString(this PersistenceExtensions<SqlPersistence> configuration, string connectionString)
        {
            var value = new Func<Task<DbConnection>>(() => SqlHelpers.New(connectionString));
            configuration.GetSettings()
                .Set("SqlPersistence.ConnectionBuilder", value);
        }

        public static void ConnectionString<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, string connectionString)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.ConnectionBuilder";
            var value = new Func<Task<DbConnection>>(() => SqlHelpers.New(connectionString));
            configuration.GetSettings()
                .Set(key, value);
        }

        internal static Func<Task<DbConnection>> GetConnectionBuilder<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetValue<Func<Task<DbConnection>>, TStorageType>("ConnectionBuilder", () => { throw new Exception("ConnectionString or ConnectionBuilder must be defined."); });
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

        internal static bool GetDisableInstaller(this ReadOnlySettings settings)
        {
            bool value;
            if (settings.TryGet("SqlPersistence.DisableInstaller", out value))
            {
                return value;
            }
            return false;
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

        internal static bool ShouldInstall<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            var featureEnabled = settings.GetFeatureEnabled<TStorageType>();
            var disableInstaller = settings.GetDisableInstaller();
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

    }
}