namespace NServiceBus.TransactionalSession
{
    using System;
    using System.Collections.Generic;
    using Transport;

    /// <summary>
    ///
    /// </summary>
    public sealed class SqlPersistenceOpenSessionOptions : OpenSessionOptions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="tenantIdHeaderName"></param>
        /// <param name="tenantId"></param>
        public SqlPersistenceOpenSessionOptions(string tenantIdHeaderName = null, string tenantId = null)
        {
            var headers = new Dictionary<string, string>();
            if (tenantIdHeaderName != null && tenantId != null)
            {
                headers.Add(tenantIdHeaderName, tenantId);
                Metadata.Add(tenantIdHeaderName, tenantId);
            }

            Extensions.Set(new IncomingMessage(SessionId, headers, Array.Empty<byte>()));
        }
    }
}