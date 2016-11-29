using System;
using Newtonsoft.Json;

namespace NServiceBus.Persistence.Sql
{
    public delegate JsonSerializerSettings VersionDeserializeBuilder(Type sagaDataType, Version sagaVersion);
}