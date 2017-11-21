#pragma warning disable CS0419
namespace NServiceBus.Persistence.Sql
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// A delegate used to configure how a specific saga data version will be serialized.
    /// <seealso cref="SagaSettings.JsonSettingsForVersion"/>
    /// </summary>
    /// <param name="sagaDataType">The saga data <see cref="Type"/> that the <see cref="Newtonsoft.Json.JsonSerializerSettings"/> are being returned for.</param>
    /// <param name="sagaVersion">The assembly version of the saga data being serialized.</param>
    public delegate JsonSerializerSettings RetrieveVersionSpecificJsonSettings(Type sagaDataType, Version sagaVersion);
}
#pragma warning restore CS0419