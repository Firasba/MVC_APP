namespace tuto.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Board : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Inventory", "BoardName", c => c.String());
            AddColumn("dbo.Inventory", "BoardType", c => c.String());
            DropColumn("dbo.Inventory", "InvName");
            DropColumn("dbo.Inventory", "InvType");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Inventory", "InvType", c => c.String());
            AddColumn("dbo.Inventory", "InvName", c => c.String());
            DropColumn("dbo.Inventory", "BoardType");
            DropColumn("dbo.Inventory", "BoardName");
        }
    }
}
