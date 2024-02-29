namespace NServiceBus.TransactionalSession
{
    using System;
    using System.Collections.Generic;
    using Transport;

    /// <summary>
    /// The options allowing to control the behavior of the transactional session.
    /// </summary>
    public sealed class SqlPersistenceOpenSessionOptions : OpenSessionOptions
    {
        /// <summary>
        /// Creates a new instance of the SqlPersistenceOpenSessionOptions.
        /// </summary>
        /// <param name="tenantInformation">An optional tenant id header name and value.</param>
        public SqlPersistenceOpenSessionOptions(
            (string tenantIdHeaderName, string tenantId) tenantInformation = default)
        {
            Dictionary<string, string> headers = null;
            if (tenantInformation != default)
            {
                headers = new Dictionary<string, string>(1)
                {
                    { tenantInformation.tenantIdHeaderName, tenantInformation.tenantId }
                };
                Metadata.Add(tenantInformation.tenantIdHeaderName, tenantInformation.tenantId);
            }

            Extensions.Set(new IncomingMessage(SessionId, headers ?? [], Array.Empty<byte>()));
        }
    }
}