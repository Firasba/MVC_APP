using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using tuto.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace tuto.DAL
{
    public class InventoryContext:DbContext

    {
        public InventoryContext() : base("InventoryContext")
        {
        }
    public DbSet<Inventory> Inventories { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }


    }
    public class InventoryRepository
    {
        private const int pageSize = 20;
        public List<Inventory> GetInventories(int? pageNumber)
        {
            var numberOfRecordToskip = pageNumber * pageSize;
            using (var context = new InventoryContext())
            {
                return context.Inventories.OrderBy(x => x.ID).Skip(Convert.ToInt32(numberOfRecordToskip)).Take(pageSize).ToList<Inventory>();
            }
        }
    }





}