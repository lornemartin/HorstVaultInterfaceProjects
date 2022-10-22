namespace RadanMaster.Migrations
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using RadanMaster.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<RadanMaster.DAL.RadanMasterContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            AutomaticMigrationDataLossAllowed = true;
        }

        protected override void Seed(RadanMaster.DAL.RadanMasterContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.

            //var parts = new List<Part>
            //{
            //    new Part { FileName = "205SP-13(Front Axle Channel)", Description = "Front Axle Channel", Material = "Steel, Mild", Thickness = 0.25 },
            //    new Part { FileName = "205SP-09(Rubber Spring Mount)", Description = "Rubber Spring Mount", Material = "Steel, Mild", Thickness = 0.25 },
            //    new Part { FileName = "205SP-10(Rubber Spring Spacer)", Description = "Rubber Spring Spacer", Material = "Steel, Mild", Thickness = 0.25 },
            //    new Part { FileName = "205SP-33(Rear Axle Channel)", Description = "Rear Axle Channel", Material = "Steel, Mild", Thickness = 0.25 },
            //    new Part { FileName = "205SP-38", Description = "Steering Arm Reinforcing", Material = "Steel, Mild", Thickness = 0.25 }
            //};

            //parts.ForEach(p => context.Parts.AddOrUpdate(p2 => new { p2.FileName }, p));
            //context.SaveChanges();

            //var orders = new List<Order>
            //{
            //    new Order { OrderNumber = "A123456", EntryDate = DateTime.Now, DueDate = DateTime.Now, IsComplete = false, IsBatch = false }
            //};

            //orders.ForEach(o => context.Orders.AddOrUpdate(o2 => new { o2.OrderNumber }, o));
            //context.SaveChanges();

            //var orderItems = new List<OrderItem>
            //{
            //    new OrderItem { Order = orders[0], IsComplete = false, QtyRequired = 1,QtyNested = 0, Part = parts[0] },
            //    new OrderItem { Order = orders[0], IsComplete = false, QtyRequired = 3,QtyNested = 0, Part = parts[1] },
            //    new OrderItem { Order = orders[0], IsComplete = false, QtyRequired = 10,QtyNested = 0, Part = parts[3] },
            //    new OrderItem { Order = orders[0], IsComplete = false, QtyRequired = 199,QtyNested = 0, Part = parts[4] }
            //};

            //orderItems.ForEach(oi => context.OrderItems.AddOrUpdate(oi2 => new { oi2.ID }, oi));
            //context.SaveChanges();


        }
    }
}
