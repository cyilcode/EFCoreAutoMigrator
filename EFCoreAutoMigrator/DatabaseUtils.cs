using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EFCoreAutoMigrator
{
    public static class DatabaseUtils
    {
        private const string CLEAR_DATABASE_SQL_SCRIPT = "TruncateDatabase.sql";

        public static bool AllMigrationsApplied(this DbContext context)
        {
            var applied = context.GetService<IHistoryRepository>()
                .GetAppliedMigrations()
                .Select(m => m.MigrationId);

            var total = context.GetService<IMigrationsAssembly>()
                .Migrations
                .Select(m => m.Key);

            return !total.Except(applied).Any();
        }

        public static void DropTables(DbContext context)
        {
            string sqlCode = Encoding.UTF8.GetString(GetResourceData(CLEAR_DATABASE_SQL_SCRIPT));
            if (string.IsNullOrEmpty(sqlCode))
            {
                throw new InvalidOperationException("Cannot find Embedded Resource [TruncateDatabase.sql]");
            }
            else
            {
                context.Database.ExecuteSqlCommand(sqlCode);

                // AzureSQL forces a transaction but local SQL's dont. So, we need this check here.
                if (context.Database.CurrentTransaction != null)
                {
                    context.Database.CommitTransaction();
                }
            }
        }

        private static byte[] GetResourceData(string resourceName)
        {
            string embeddedResource = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceNames()
                .FirstOrDefault(resource => resource.Contains(resourceName));

            if (!string.IsNullOrWhiteSpace(embeddedResource))
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedResource))
                {
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return data;
                }
            }

            return null;
        }
    }
}
