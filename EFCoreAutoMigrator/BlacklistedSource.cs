using System;

namespace EFCoreAutoMigrator
{
    public class BlacklistedSource : DataSourceBase
    {
        public BlacklistedSource(string serverAddress, string databaseName)
        {
            if (string.IsNullOrEmpty(serverAddress))
            {
                throw new ArgumentException("Required parameter", nameof(serverAddress));
            }

            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentException("Required parameter", nameof(databaseName));
            }

            base.ServerAddress = serverAddress.ToUpperInvariant();
            base.DatabaseName = databaseName.ToUpperInvariant();
        }
    }
}
