namespace tuto.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Manufacturer : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Inventory", "ManufacturerData", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Inventory", "ManufacturerData");
        }
    }
}
