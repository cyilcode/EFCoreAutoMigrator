using System;

namespace EFCoreAutoMigrator
{
    public class AutoMigratorOptions
    {
        /// <summary>
        /// Stores the name of database table for <see cref="MigrationModelHashStorageMode.Database"/>.
        /// Its default value is __dbMetadata.
        /// </summary>
        public string MigrationHashTableName { get; set; } = "__dbMetadata";

        /// <summary>
        /// Stores the name of file for <see cref="MigrationModelHashStorageMode.File"/>.
        /// Its default value is automigration.amhs
        /// </summary>
        public string MigrationHashFileName { get; set; } = "automigration.amhs";

        /// <summary>
        /// Toggles the <see cref="Microsoft.Extensions.Logging.ILogger"/> usage.
        /// </summary>
        public bool LoggingEnabled { get; set; } = true;

        /// <summary>
        /// Stores the migration file path for <see cref="MigrationModelHashStorageMode.File"/>.
        /// </summary>
        public string MigrationFilePath { get; set; }

        public AutoMigratorOptions()
        {
            this.MigrationFilePath = $"{Environment.CurrentDirectory}\\{this.MigrationHashTableName}";
        }
    }
}
