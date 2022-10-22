namespace RadanMaster.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class tweakedOrderItemOperation : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.OrderItemOperations", "OrderItem_ID", "dbo.OrderItems");
            DropIndex("dbo.OrderItemOperations", new[] { "OrderItem_ID" });
            RenameColumn(table: "dbo.OrderItemOperations", name: "OrderItem_ID", newName: "orderItemID");
            AlterColumn("dbo.OrderItemOperations", "orderItemID", c => c.Int(nullable: false));
            CreateIndex("dbo.OrderItemOperations", "orderItemID");
            AddForeignKey("dbo.OrderItemOperations", "orderItemID", "dbo.OrderItems", "ID", cascadeDelete: false);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.OrderItemOperations", "orderItemID", "dbo.OrderItems");
            DropIndex("dbo.OrderItemOperations", new[] { "orderItemID" });
            AlterColumn("dbo.OrderItemOperations", "orderItemID", c => c.Int());
            RenameColumn(table: "dbo.OrderItemOperations", name: "orderItemID", newName: "OrderItem_ID");
            CreateIndex("dbo.OrderItemOperations", "OrderItem_ID");
            AddForeignKey("dbo.OrderItemOperations", "OrderItem_ID", "dbo.OrderItems", "ID");
        }
    }
}
