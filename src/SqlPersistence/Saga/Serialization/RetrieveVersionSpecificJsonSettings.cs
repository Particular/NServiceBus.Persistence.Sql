using System;
using Newtonsoft.Json;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// A delegate used to configure how a specific saga data version will be serialized.
    /// <seealso cref="SagaSettings.JsonSettingsForVersion"/>
    /// </summary>
    /// <param name="sagaDataType">The saga data <see cref="Type"/> that the <see cref="Newtonsoft.Json.JsonSerializerSettings"/> are being returned for.</param>
    /// <param name="sagaVersion">The assembly version of the saga data being serialized.</param>
    public delegate JsonSerializerSettings RetrieveVersionSpecificJsonSettings(Type sagaDataType, Version sagaVersion);
}