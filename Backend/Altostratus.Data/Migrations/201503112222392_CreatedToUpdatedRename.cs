namespace Altostratus.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreatedToUpdatedRename : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Conversations", "LastUpdated", c => c.DateTime(nullable: false));
            DropColumn("dbo.Conversations", "Created");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Conversations", "Created", c => c.DateTime(nullable: false));
            DropColumn("dbo.Conversations", "LastUpdated");
        }
    }
}
