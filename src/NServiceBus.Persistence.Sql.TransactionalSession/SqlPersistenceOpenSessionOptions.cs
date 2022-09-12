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
            if (tenantIdHeaderName != null)
            {
                if (string.IsNullOrEmpty(tenantId))
                {
                    throw new Exception("A tenant header is available, but the value is missing.");
                }
                headers.Add(tenantIdHeaderName, tenantId);
                Metadata.Add(tenantIdHeaderName, tenantId);
            }

            Extensions.Set(new IncomingMessage(SessionId, headers, Array.Empty<byte>()));
        }
    }
}