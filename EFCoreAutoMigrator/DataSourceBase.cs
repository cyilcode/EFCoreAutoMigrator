namespace EFCoreAutoMigrator
{
    public abstract class DataSourceBase
    {
        public string ServerAddress { get; protected set; }

        public string DatabaseName { get; protected set; }
    }
}
