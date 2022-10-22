namespace RadanMaster.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Files",
                c => new
                    {
                        FileId = c.Int(nullable: false, identity: true),
                        FileName = c.String(maxLength: 255),
                        ContentType = c.String(maxLength: 100),
                        Content = c.Binary(),
                        FileType = c.Int(nullable: false),
                        CreationDate = c.DateTime(nullable: false),
                        Part_ID = c.Int(),
                    })
                .PrimaryKey(t => t.FileId)
                .ForeignKey("dbo.Parts", t => t.Part_ID)
                .Index(t => t.Part_ID);
            
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
                        Title = c.String(),
                        ParentPartNumber = c.String(),
                        CategoryName = c.String(),
                        StructuralCode = c.String(),
                        PlantID = c.String(),
                        IsStock = c.Boolean(nullable: false),
                        RequiresPDF = c.Boolean(nullable: false),
                        Comment = c.String(),
                        ModifiedDate = c.DateTime(nullable: false),
                        State = c.String(),
                        Keywords = c.String(),
                        Notes = c.String(),
                        Revision = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Operations",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Location = c.String(),
                        PartID = c.Int(nullable: false),
                        isFinalOp = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Parts", t => t.PartID, cascadeDelete: true)
                .Index(t => t.PartID);
            
            CreateTable(
                "dbo.Materials",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Thickness = c.Single(nullable: false),
                        StructuralCode = c.String(),
                    })
                .PrimaryKey(t => t.ID);
            
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
                        ProductName = c.String(),
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
                "dbo.OrderItemOperations",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        qtyRequired = c.Int(nullable: false),
                        qtyDone = c.Int(nullable: false),
                        operationID = c.Int(nullable: false),
                        OrderItem_ID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Operations", t => t.operationID, cascadeDelete: true)
                .ForeignKey("dbo.OrderItems", t => t.OrderItem_ID)
                .Index(t => t.operationID)
                .Index(t => t.OrderItem_ID);
            
            CreateTable(
                "dbo.OperationPerformeds",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        qtyDone = c.Int(nullable: false),
                        timePerformed = c.DateTime(nullable: false),
                        Notes = c.String(),
                        usr_UserID = c.Int(),
                        OrderItemOperation_ID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Users", t => t.usr_UserID)
                .ForeignKey("dbo.OrderItemOperations", t => t.OrderItemOperation_ID)
                .Index(t => t.usr_UserID)
                .Index(t => t.OrderItemOperation_ID);
            
            CreateTable(
                "dbo.OrderItemOperationPerformeds",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        qtyRequired = c.Int(nullable: false),
                        qtyDone = c.Int(nullable: false),
                        orderOperationID = c.Int(nullable: false),
                        operationPerformedID = c.Int(nullable: false),
                        orderItemOperation_ID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.OperationPerformeds", t => t.operationPerformedID, cascadeDelete: true)
                .ForeignKey("dbo.OrderItemOperations", t => t.orderItemOperation_ID)
                .Index(t => t.operationPerformedID)
                .Index(t => t.orderItemOperation_ID);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserID = c.Int(nullable: false, identity: true),
                        UserName = c.String(),
                        Password = c.String(),
                    })
                .PrimaryKey(t => t.UserID);
            
            CreateTable(
                "dbo.Privileges",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        buttonName = c.String(),
                        HasAccess = c.Boolean(nullable: false),
                        User_UserID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Users", t => t.User_UserID)
                .Index(t => t.User_UserID);
            
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
            DropForeignKey("dbo.OrderItemOperations", "OrderItem_ID", "dbo.OrderItems");
            DropForeignKey("dbo.OperationPerformeds", "OrderItemOperation_ID", "dbo.OrderItemOperations");
            DropForeignKey("dbo.OperationPerformeds", "usr_UserID", "dbo.Users");
            DropForeignKey("dbo.Privileges", "User_UserID", "dbo.Users");
            DropForeignKey("dbo.OrderItemOperationPerformeds", "orderItemOperation_ID", "dbo.OrderItemOperations");
            DropForeignKey("dbo.OrderItemOperationPerformeds", "operationPerformedID", "dbo.OperationPerformeds");
            DropForeignKey("dbo.OrderItemOperations", "operationID", "dbo.Operations");
            DropForeignKey("dbo.OrderItems", "OrderID", "dbo.Orders");
            DropForeignKey("dbo.NestOrderItems", "OrderItem_ID", "dbo.OrderItems");
            DropForeignKey("dbo.NestOrderItems", "Nest_ID", "dbo.Nests");
            DropForeignKey("dbo.NestedParts", "Nest_ID", "dbo.Nests");
            DropForeignKey("dbo.Operations", "PartID", "dbo.Parts");
            DropForeignKey("dbo.Files", "Part_ID", "dbo.Parts");
            DropIndex("dbo.NestOrderItems", new[] { "OrderItem_ID" });
            DropIndex("dbo.NestOrderItems", new[] { "Nest_ID" });
            DropIndex("dbo.RadanIDs", new[] { "ID" });
            DropIndex("dbo.Privileges", new[] { "User_UserID" });
            DropIndex("dbo.OrderItemOperationPerformeds", new[] { "orderItemOperation_ID" });
            DropIndex("dbo.OrderItemOperationPerformeds", new[] { "operationPerformedID" });
            DropIndex("dbo.OperationPerformeds", new[] { "OrderItemOperation_ID" });
            DropIndex("dbo.OperationPerformeds", new[] { "usr_UserID" });
            DropIndex("dbo.OrderItemOperations", new[] { "OrderItem_ID" });
            DropIndex("dbo.OrderItemOperations", new[] { "operationID" });
            DropIndex("dbo.OrderItems", new[] { "PartID" });
            DropIndex("dbo.OrderItems", new[] { "OrderID" });
            DropIndex("dbo.NestedParts", new[] { "OrderItem_ID" });
            DropIndex("dbo.NestedParts", new[] { "Nest_ID" });
            DropIndex("dbo.Operations", new[] { "PartID" });
            DropIndex("dbo.Files", new[] { "Part_ID" });
            DropTable("dbo.NestOrderItems");
            DropTable("dbo.RadanIDs");
            DropTable("dbo.Privileges");
            DropTable("dbo.Users");
            DropTable("dbo.OrderItemOperationPerformeds");
            DropTable("dbo.OperationPerformeds");
            DropTable("dbo.OrderItemOperations");
            DropTable("dbo.Orders");
            DropTable("dbo.Nests");
            DropTable("dbo.OrderItems");
            DropTable("dbo.NestedParts");
            DropTable("dbo.Materials");
            DropTable("dbo.Operations");
            DropTable("dbo.Parts");
            DropTable("dbo.Files");
        }
    }
}
