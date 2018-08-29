namespace ItemExport
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Core.EntityClient;
    using System.Data.Entity.Infrastructure;

    public partial class HorstMFGEntities : DbContext
    {
        public HorstMFGEntities(string connectionString)
            : base(connectionString)
        {
        }

        public static string GetEntityConnectionString(string connectionString)
        {
            var entityBuilder = new EntityConnectionStringBuilder();

            entityBuilder.Provider = "System.Data.SqlClient";
            entityBuilder.ProviderConnectionString = connectionString + ";MultipleActiveResultSets=True;App=EntityFramework;";
            entityBuilder.Metadata = @"res://*/Model1.csdl|res://*/Model1.ssdl|res://*/Model1.msl";

            return entityBuilder.ToString();
        }
    }
}

