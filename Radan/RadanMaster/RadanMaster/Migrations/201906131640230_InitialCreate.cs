namespace RadanMaster.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.NestedParts",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Qty = c.Int(nullable: false),
                        Nest_ID = c.Int(),
                        OrderItem_ID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Nests", t => t.Nest_ID)
                .ForeignKey("dbo.OrderItems", t => t.OrderItem_ID)
                .Index(t => t.Nest_ID)
                .Index(t => t.OrderItem_ID);
            
            CreateTable(
                "dbo.OrderItems",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        OrderID = c.Int(nullable: false),
                        IsComplete = c.Boolean(nullable: false),
                        QtyRequired = c.Int(nullable: false),
                        QtyNested = c.Int(nullable: false),
                        IsInProject = c.Boolean(nullable: false),
                        Notes = c.String(),
                        PartID = c.Int(nullable: false),
                        RadanIDNumber = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Orders", t => t.OrderID, cascadeDelete: true)
                .ForeignKey("dbo.Parts", t => t.PartID, cascadeDelete: true)
                .Index(t => t.OrderID)
                .Index(t => t.PartID);
            
            CreateTable(
                "dbo.Nests",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        nestName = c.String(),
                        nestPath = c.String(),
                        Thumbnail = c.Binary(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Orders",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        OrderNumber = c.String(),
                        ScheduleName = c.String(),
                        BatchName = c.String(),
                        EntryDate = c.DateTime(nullable: false),
                        DueDate = c.DateTime(nullable: false),
                        IsComplete = c.Boolean(nullable: false),
                        IsBatch = c.Boolean(nullable: false),
                        DateCompleted = c.DateTime(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Parts",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        FileName = c.String(),
                        Description = c.String(),
                        Material = c.String(),
                        Thickness = c.Double(nullable: false),
                        Thumbnail = c.Binary(),
                        HasBends = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.RadanIDs",
                c => new
                    {
                        ID = c.Int(nullable: false),
                        RadanIDNumber = c.Int(nullable: false),
                        OrderItemID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.OrderItems", t => t.ID)
                .Index(t => t.ID);
            
            CreateTable(
                "dbo.NestOrderItems",
                c => new
                    {
                        Nest_ID = c.Int(nullable: false),
                        OrderItem_ID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Nest_ID, t.OrderItem_ID })
                .ForeignKey("dbo.Nests", t => t.Nest_ID, cascadeDelete: true)
                .ForeignKey("dbo.OrderItems", t => t.OrderItem_ID, cascadeDelete: true)
                .Index(t => t.Nest_ID)
                .Index(t => t.OrderItem_ID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.NestedParts", "OrderItem_ID", "dbo.OrderItems");
            DropForeignKey("dbo.RadanIDs", "ID", "dbo.OrderItems");
            DropForeignKey("dbo.OrderItems", "PartID", "dbo.Parts");
            DropForeignKey("dbo.OrderItems", "OrderID", "dbo.Orders");
            DropForeignKey("dbo.NestOrderItems", "OrderItem_ID", "dbo.OrderItems");
            DropForeignKey("dbo.NestOrderItems", "Nest_ID", "dbo.Nests");
            DropForeignKey("dbo.NestedParts", "Nest_ID", "dbo.Nests");
            DropIndex("dbo.NestOrderItems", new[] { "OrderItem_ID" });
            DropIndex("dbo.NestOrderItems", new[] { "Nest_ID" });
            DropIndex("dbo.RadanIDs", new[] { "ID" });
            DropIndex("dbo.OrderItems", new[] { "PartID" });
            DropIndex("dbo.OrderItems", new[] { "OrderID" });
            DropIndex("dbo.NestedParts", new[] { "OrderItem_ID" });
            DropIndex("dbo.NestedParts", new[] { "Nest_ID" });
            DropTable("dbo.NestOrderItems");
            DropTable("dbo.RadanIDs");
            DropTable("dbo.Parts");
            DropTable("dbo.Orders");
            DropTable("dbo.Nests");
            DropTable("dbo.OrderItems");
            DropTable("dbo.NestedParts");
        }
    }
}
