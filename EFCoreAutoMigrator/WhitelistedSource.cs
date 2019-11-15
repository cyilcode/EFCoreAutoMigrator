namespace EFCoreAutoMigrator
{
    public sealed class WhitelistedSource : BlacklistedSource
    {
        public WhitelistedSource(string serverAddress, string databaseName)
            : base(serverAddress, databaseName)
        {
        }
    }
}
