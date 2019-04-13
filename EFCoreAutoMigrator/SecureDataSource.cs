using System;

namespace EFCoreAutoMigrator
{
    public sealed class SecureDataSource
    {
        public SecureDataSource(string serverAddress, string databaseName)
        {
            if (string.IsNullOrEmpty(serverAddress))
            {
                throw new ArgumentException("Required parameter", nameof(serverAddress));
            }

            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentException("Required parameter", nameof(databaseName));
            }

            this.ServerAddress = serverAddress.ToUpperInvariant();
            this.DatabaseName = databaseName.ToUpperInvariant();
        }

        public string ServerAddress { get; private set; }

        public string DatabaseName { get; private set; }
    }
}
