namespace Altostratus.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial10 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Categories",
                c => new
                    {
                        CategoryID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.CategoryID);
            
            CreateTable(
                "dbo.Conversations",
                c => new
                    {
                        ConversationId = c.Int(nullable: false, identity: true),
                        Url = c.String(nullable: false, maxLength: 895, unicode: false),
                        Created = c.DateTime(nullable: false),
                        DbUpdated = c.DateTime(nullable: false),
                        Title = c.String(maxLength: 500),
                        Body = c.String(),
                        ProviderID = c.Int(nullable: false),
                        CategoryID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ConversationId)
                .ForeignKey("dbo.Categories", t => t.CategoryID, cascadeDelete: true)
                .ForeignKey("dbo.Providers", t => t.ProviderID, cascadeDelete: true)
                .Index(t => new { t.Url, t.CategoryID }, unique: true, name: "UrlAndCategoryIdIndex")
                .Index(t => t.ProviderID);
            
            CreateTable(
                "dbo.Providers",
                c => new
                    {
                        ProviderID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.ProviderID);
            
            CreateTable(
                "dbo.Tags",
                c => new
                    {
                        TagID = c.Int(nullable: false, identity: true),
                        CategoryID = c.Int(nullable: false),
                        Value = c.String(maxLength: 250),
                        ProviderID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.TagID)
                .ForeignKey("dbo.Categories", t => t.CategoryID, cascadeDelete: true)
                .ForeignKey("dbo.Providers", t => t.ProviderID, cascadeDelete: true)
                .Index(t => t.CategoryID)
                .Index(t => t.ProviderID);
            
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.UserCategories",
                c => new
                    {
                        UserCategoryId = c.Int(nullable: false, identity: true),
                        ApplicationUser_Id = c.String(maxLength: 128),
                        CategoryID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.UserCategoryId)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUser_Id)
                .ForeignKey("dbo.Categories", t => t.CategoryID, cascadeDelete: true)
                .ForeignKey("dbo.UserPreferences", t => t.ApplicationUser_Id)
                .Index(t => t.ApplicationUser_Id)
                .Index(t => t.CategoryID);
            
            CreateTable(
                "dbo.AspNetUsers",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.UserPreferences",
                c => new
                    {
                        ApplicationUser_Id = c.String(nullable: false, maxLength: 128),
                        ConversationLimit = c.Int(nullable: false),
                        SortOrder = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ApplicationUser_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUser_Id)
                .Index(t => t.ApplicationUser_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserCategories", "ApplicationUser_Id", "dbo.UserPreferences");
            DropForeignKey("dbo.UserPreferences", "ApplicationUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.UserCategories", "CategoryID", "dbo.Categories");
            DropForeignKey("dbo.UserCategories", "ApplicationUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.Tags", "ProviderID", "dbo.Providers");
            DropForeignKey("dbo.Tags", "CategoryID", "dbo.Categories");
            DropForeignKey("dbo.Conversations", "ProviderID", "dbo.Providers");
            DropForeignKey("dbo.Conversations", "CategoryID", "dbo.Categories");
            DropIndex("dbo.UserPreferences", new[] { "ApplicationUser_Id" });
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.UserCategories", new[] { "CategoryID" });
            DropIndex("dbo.UserCategories", new[] { "ApplicationUser_Id" });
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropIndex("dbo.Tags", new[] { "ProviderID" });
            DropIndex("dbo.Tags", new[] { "CategoryID" });
            DropIndex("dbo.Conversations", new[] { "ProviderID" });
            DropIndex("dbo.Conversations", "UrlAndCategoryIdIndex");
            DropTable("dbo.UserPreferences");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.UserCategories");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetRoles");
            DropTable("dbo.Tags");
            DropTable("dbo.Providers");
            DropTable("dbo.Conversations");
            DropTable("dbo.Categories");
        }
    }
}
