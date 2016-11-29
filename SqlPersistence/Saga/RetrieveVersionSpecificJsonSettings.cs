using System;
using Newtonsoft.Json;

namespace NServiceBus.Persistence.Sql
{
    public delegate JsonSerializerSettings RetrieveVersionSpecificJsonSettings(Type sagaDataType, Version sagaVersion);
}