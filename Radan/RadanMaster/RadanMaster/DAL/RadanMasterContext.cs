using RadanMaster.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace RadanMaster.DAL
{
    public class RadanMasterContext : DbContext
    {
        public RadanMasterContext() : base("RadanMasterContext")
        {
        }

        public DbSet<Part> Parts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Nest> Nests { get; set; }
        public DbSet<NestedParts> NestedParts { get; set; }
        public DbSet<RadanID> RadanIDs { get; set; }
    }

}
