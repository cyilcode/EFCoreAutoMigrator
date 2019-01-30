using System;

namespace EFCoreAutoMigrator
{
    public class AutoMigratorOptions
    {
        public string MigrationHashTableName { get; set; } = "__dbMetadata";

        public string MigrationHashFileName { get; set; } = "automigration.sign";

        public bool LoggingEnabled { get; set; } = true;

        public string MigrationFilePath { get; set; }

        public AutoMigratorOptions()
        {
            this.MigrationFilePath = $"{Environment.CurrentDirectory}\\{this.MigrationHashTableName}";
        }
    }
}
