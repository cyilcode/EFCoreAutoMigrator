using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

[assembly: SuppressMessage("LocalUse", "EF1000", Justification = "Development Tool")]
namespace EFCoreAutoMigrator
{
    /// <summary>
    /// Modes for database migration hash storage method.
    /// </summary>
    public enum MigrationModelHashStorageMode
    {
        /// <summary>
        /// Stores the created hash in your file system. This can be configured with <see cref="AutoMigratorOptions"/> class.
        /// </summary>
        File = 0,
        /// <summary>
        /// Stores the created hash in your database. This can be configured with <see cref="AutoMigratorOptions"/> class.
        /// </summary>
        Database = 1
    }

    public class AutoMigrator
    {
        private readonly ILogger _logger;
        private readonly DbContext _dbContext;
        private readonly AutoMigratorOptions _options;

        /// <summary>
        /// Creates a new instance of AutoMigrator with given DbContext and settings.
        /// </summary>
        /// <param name="dbContext">DbContext to perform migration operations.</param>
        /// <param name="blacklistedSources">Secure data sources to prevent accidental auto migrator initiations.</param>
        /// <param name="options">Configurations for database migration and hash storage operations.</param>
        /// <param name="logger">Logger for console or text outputs.</param>
        public AutoMigrator(DbContext dbContext, BlacklistedSource[] blacklistedSources, AutoMigratorOptions options = null, ILogger logger = null)
        {
            _logger = logger;
            _options = options ?? new AutoMigratorOptions();
            _dbContext = dbContext ?? throw new ArgumentNullException("DbContext is required.");
            CheckDataSource(dbContext, blacklistedSources, true);
        }

        /// <summary>
        /// Creates a new instance of AutoMigrator with given DbContext and settings.
        /// </summary>
        /// <param name="dbContext">DbContext to perform migration operations.</param>
        /// <param name="whitelistedSources">Insecure Data Sources to allow the AutoMigration process execute. EFCoreAutoMigrator will not execute any data source besides the ones that are provided in this list.</param>
        /// <param name="options">Configurations for database migration and hash storage operations.</param>
        /// <param name="logger">Logger for console or text outputs.</param>
        public AutoMigrator(DbContext dbContext, WhitelistedSource[] whitelistedSources, AutoMigratorOptions options = null, ILogger logger = null)
        {
            _logger = logger;
            _options = options ?? new AutoMigratorOptions();
            _dbContext = dbContext ?? throw new ArgumentNullException("DbContext is required.");
            CheckDataSource(dbContext, whitelistedSources, false);
        }

        /// <summary>
        /// Migrates your database schema to your database with the given settings.
        /// </summary>
        /// <param name="forceMigration">Forces the migration process even if there are no changes in the database schema.</param>
        /// <param name="mode">Storage method of created schema hash.</param>
        /// <param name="postMigration">Action reference for post migration process.</param>
        public void Migrate(bool forceMigration, MigrationModelHashStorageMode mode, Action postMigration = null)
        {
            // Get the existing model hash.
            string currentHash = GetCurrentHash(mode);

            // Create the database generation script.
            string generatedScript = _dbContext.Database.GenerateCreateScript();

            // Create a new hash from database generation script.
            string newHash = Sha256(generatedScript);

            // Check for a change
            if (newHash != currentHash || forceMigration)
            {
                // Woop-woop we have a change !
                Console.WriteLine();
                WriteMessage("DB model change detected. Executing migration..");
                WriteMessage("Dropping all tables...");

                // Drop all tables instead of dropping the database since some cloud providers are not compatible with that.
                DatabaseUtils.DropTables(_dbContext);
                WriteMessage("Creating new tables...");

                // Create new tables from the generated script.
                CreateNewTables(generatedScript, mode);
                WriteMessage("Updating model hash...");

                // Update the script hash.
                UpdateScriptHash(newHash, mode);
                WriteMessage("Database has been migrated successfully..");

                // Check if we need to invoke a post process function.
                if (postMigration != null)
                {
                    ExecutePostMigration(postMigration);
                }
            }
        }

        private void ExecutePostMigration(Action postMigration)
        {
            WriteMessage("Executing database post process function...");
            postMigration.Invoke();
            WriteMessage("Migration sequence has been completed.");
            Console.WriteLine();
        }

        private string DbMetadataModel =>
           $@"CREATE TABLE [dbo].[{_options.MigrationHashTableName}](
                [Hash] [nvarchar](max) NOT NULL
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]";

        private string Sha256(string rawData)
        {
            using (var sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private void WriteMessage(string message, bool includePrefix = true)
        {
            if (_options.LoggingEnabled)
            {

                string messageToWrite = includePrefix ? string.Concat("[EFCoreAutoMigrator] - ", message) : message;
                if (_logger != null)
                {
                    _logger.LogInformation(messageToWrite);
                }
                else
                {
                    Console.WriteLine(messageToWrite);
                }
            }
        }

        private string GetCurrentHash(MigrationModelHashStorageMode mode)
        {
            if (mode == MigrationModelHashStorageMode.File)
            {
                if (File.Exists(_options.MigrationFilePath))
                {
                    return File.ReadAllText(_options.MigrationFilePath);
                }

                // No migration signature file. Just return empty.
                return string.Empty;
            }
            else
            {
                // Create a connection from the context
                DbConnection conn = _dbContext.Database.GetDbConnection();
                _dbContext.Database.CurrentTransaction?.Commit();

                // Check for the connection state since it might be open already.
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                // Execute a query to retrieve information about MIGRATION_SIGNATURE_DB_TABLE_NAME table.
                DbDataReader reader = ExecuteReaderCommand(
                    "select * from sysobjects where name=@tableName and xtype=@xType",
                    new SqlParameter[] { new SqlParameter("@tableName", _options.MigrationHashTableName), new SqlParameter("@xType", "U") },
                    conn);

                // Do we have the table ?
                if (reader.HasRows)
                {
                    // yep we do.
                    reader.Close();

                    // Read the hash from the table.
                    reader = ExecuteReaderCommand($"select TOP 1 [Hash] from {_options.MigrationHashTableName}", null, conn);

                    // Do we have a value in it ?
                    if (reader.HasRows)
                    {
                        reader.Read();
                        // Get the first column since we only read 1 column and we dont need to worry about null value here since the column is not nullable.
                        string retrievedHash = reader.GetString(0);
                        reader.Close();
                        return retrievedHash;
                    }
                    else
                    {
                        // Nope just return empty.
                        reader.Close();
                        return string.Empty;
                    }
                }
                else
                {
                    // Nope just return empty.
                    reader.Close();
                    return string.Empty;
                }
            }
        }

        private void UpdateScriptHash(string newHash, MigrationModelHashStorageMode mode)
        {
            if (mode == MigrationModelHashStorageMode.Database)
            {
                _dbContext.Database.ExecuteSqlRaw($"INSERT INTO {_options.MigrationHashTableName} (Hash) VALUES (@hash)", new SqlParameter("@hash", newHash));
            }
            else
            {
                File.WriteAllText(_options.MigrationFilePath, newHash);
            }
        }

        private void CreateNewTables(string dbScript, MigrationModelHashStorageMode mode)
        {
            string executionScript;
            if (mode == MigrationModelHashStorageMode.Database)
            {
                // Prepend create table script.
                executionScript = string.Join(Environment.NewLine, new string[] { DbMetadataModel, dbScript });
            }
            else
            {
                executionScript = dbScript;
            }

            // Remove GO instructions since they are not compatible with ExecuteSqlCommand.
            _dbContext.Database.ExecuteSqlRaw(executionScript.Replace("GO", string.Empty));
        }

        private void CheckDataSource(DbContext dbContext, DataSourceBase[] dataSources, bool checkForBlacklist)
        {
            DbConnection dbConnection = dbContext.Database.GetDbConnection();
            string serverAddress = dbConnection.DataSource.ToUpperInvariant();
            string databaseName = dbConnection.Database.ToUpperInvariant();
            bool isSourceListed = dataSources.Any(x => x.ServerAddress == serverAddress && x.DatabaseName == databaseName);

            if (isSourceListed && checkForBlacklist)
            {
                throw new SecureDataSourceException("Cannot execute auto migration on a blacklisted server.");
            }
            else if (!checkForBlacklist && !isSourceListed)
            {
                throw new SecureDataSourceException("Cannot find the server in the whitelist.");
            }
        }

        private DbDataReader ExecuteReaderCommand(string command, Array parameters, DbConnection connection)
        {
            DbCommand dbCommand = connection.CreateCommand();
            dbCommand.CommandText = command;
            if (parameters != null)
            {
                dbCommand.Parameters.AddRange(parameters);
            }

            DbDataReader dataReader = dbCommand.ExecuteReader();
            return dataReader;
        }
    }
}
