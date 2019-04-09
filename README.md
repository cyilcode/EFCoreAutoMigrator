[![NuGet](https://img.shields.io/nuget/dt/EFCoreAutoMigrator.svg)](https://www.nuget.org/packages/EFCoreAutoMigrator/)
[![DynamicQueryBuilder](https://img.shields.io/nuget/v/EFCoreAutoMigrator.svg)](https://www.nuget.org/packages/EFCoreAutoMigrator/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/oplog/EFCoreAutoMigrator/blob/master/LICENSE)

# What ?

EFCoreAutoMigrator is an aggressive automatic migration tool that works with [Entity Framework Core 2.1](https://github.com/aspnet/EntityFrameworkCore) and above. It basically recreates your database tables whenever it detects a change in your database schema.

# Why ?

EFCoreAutoMigrator was built for speeding up the development stages of your applications. Since you don't need to deal with database migrations after you install this tool;

* No migration files generated.
* No more conflicts with your branch and your teammates.
* Fixing database schema errors is a lot easier.

# How ?

EFCoreAutoMigrator creates and stores a hash in your FileSystem or Database to check if you have any changes on your current schema by utilizing [GenerateCreateScript ](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.relationaldatabasefacadeextensions.generatecreatescript?view=efcore-2.1) function of EFCore 2.1+.

#### Installation
It is quite simple to install.

You can install EFCoreAutoMigrator from NuGet with the command below:

`Install-Package EFCoreAutoMigrator`

### On ASP.NET Core Project

**Program.cs**
```csharp
public static class Program
    {
        public static void Main(string[] args)
        {
            IWebHost server = BuildWebHost(args);
            var env = server.Services.GetService(typeof(IHostingEnvironment)) as IHostingEnvironment;

            // I would highly suggest you to not to use this tool in your production environment.
            // Since it will vaporize your whole database which you probably don't want :)
            if (!env.IsProduction())
            {
                using (IServiceScope serviceScope = server.Services.GetService<IServiceScopeFactory>().CreateScope())
                {
                    // Make sure that your DbContext is registered in your DI container.
                    MyDbContext myDbContext = serviceScope.ServiceProvider.GetService<MyDbContext>();
                    new AutoMigrator(myDbContext).EnableAutoMigration(
                        false, MigrationModelHashStorageMode.Database, () =>
                    {
                        // Seed function here if you need
                    });
                }
            }

            server.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
```
#### TODO: 
    * Tests
    * Further documentation
