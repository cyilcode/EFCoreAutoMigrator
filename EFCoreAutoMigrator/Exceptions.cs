using System;

namespace EFCoreAutoMigrator
{
    public class EFCoreAutoMigratorException : Exception
    {
        public EFCoreAutoMigratorException(string message)
            : base(message) { }
    }

    public class SecureDataSourceException : EFCoreAutoMigratorException
    {
        public SecureDataSourceException(string message)
            : base(message) { }
    }
}
