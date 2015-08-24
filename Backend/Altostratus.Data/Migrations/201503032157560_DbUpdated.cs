namespace Altostratus.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DbUpdated : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Conversations", "DbUpdated", c => c.DateTimeOffset(nullable: false, precision: 7));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Conversations", "DbUpdated", c => c.DateTime(nullable: false));
        }
    }
}
