namespace Altostratus.DAL.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    public class Configuration : DbMigrationsConfiguration<Altostratus.DAL.ApplicationDbContext>
    {
        private readonly bool _pendingMigrations;
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            var migrator = new DbMigrator(this);
            _pendingMigrations = migrator.GetPendingMigrations().Any();
        }

        protected override void Seed(Altostratus.DAL.ApplicationDbContext context)
        {
            const string Azure = "Azure";
            const string ASPNET = "ASP.NET";
            const string StackOverflow = "StackOverflow";
            const string Twitter = "Twitter";
            
           // If there are no migrations and the Tags table has been seeded, return
            if (!_pendingMigrations && context.Tags.Any())
            {
               return;
            }
           
            context.Categories.AddOrUpdate(
                x => x.Name,
                new Category { Name = Azure },
                new Category { Name = ASPNET }
                );

            context.Providers.AddOrUpdate(
                x => x.Name,
                new Provider { Name = StackOverflow },
                new Provider { Name = Twitter }
                );

            context.SaveChanges();

            int AzureCatID = context.Categories.Single(c => c.Name == Azure).CategoryID;
            int ASPNET_CatID = context.Categories.Single(c => c.Name == ASPNET).CategoryID;
            int StackOverflowProviderID = context.Providers.Single(p => p.Name == StackOverflow).ProviderID;
            int TwitterProviderID = context.Providers.Single(p => p.Name == Twitter).ProviderID;

           // The code below works via the trick that on a new DB SQL will insert rows with ID starting with 1. If any of the
           // seeded data below is deleted, the next time Seed is called it will not match ID=1, and will insert that row again. You'll get duplicate
           // rows for each row below that has been deleted. SQL actually ignores the TagID you use below when inserting, so specifying TagID is
           // misleading at best. Correctly guessing the TagID does prevent that same data from being inserted the next time the
           // Seed method is called (as long as the row is not deleted). The TagID is only used to check if the record exits. You can change 
           // the code below to 
           // new Tag { TagID = 91,  CategoryID = AzureCatID, Value = "azure-webjobs", ProviderID = StackOverflowProviderID },
           // new Tag { TagID = 29,  CategoryID = AzureCatID, Value = "#azurewebjobs", ProviderID = TwitterProviderID },
           // and TagID will be ingored for the first insert, the first Id will be 1, not 91

            context.Tags.AddOrUpdate(
                x => x.TagID,
                new Tag { TagID = 1,  CategoryID = AzureCatID, Value = "azure-webjobs", ProviderID = StackOverflowProviderID },
                new Tag { TagID = 2,  CategoryID = AzureCatID, Value = "#azurewebjobs", ProviderID = TwitterProviderID },
                new Tag { TagID = 3,  CategoryID = AzureCatID, Value = "azure-web-sites", ProviderID = StackOverflowProviderID },
                new Tag { TagID = 4,  CategoryID = AzureCatID, Value = "#azurewebsites", ProviderID = TwitterProviderID },
                new Tag { TagID = 5,  CategoryID = AzureCatID, Value = "sql-azure", ProviderID = StackOverflowProviderID },
                new Tag { TagID = 6,  CategoryID = AzureCatID, Value = "#sqlazure", ProviderID = TwitterProviderID },
                new Tag { TagID = 7,  CategoryID = ASPNET_CatID, Value = "asp.net-mvc", ProviderID = StackOverflowProviderID },
                new Tag { TagID = 8,  CategoryID = ASPNET_CatID, Value = "#aspnetmvc", ProviderID = TwitterProviderID },
                new Tag { TagID = 9,  CategoryID = ASPNET_CatID, Value = "asp.net-mvc-5", ProviderID = StackOverflowProviderID },
                new Tag { TagID = 10,  CategoryID = ASPNET_CatID, Value = "#aspnetmvc5", ProviderID = TwitterProviderID },
                new Tag { TagID = 11,  CategoryID = ASPNET_CatID, Value = "asp.net-web-api", ProviderID = StackOverflowProviderID },
                new Tag { TagID = 12,  CategoryID = ASPNET_CatID, Value = "#aspnetwebapi", ProviderID = TwitterProviderID }

                );
        }
    }
}
