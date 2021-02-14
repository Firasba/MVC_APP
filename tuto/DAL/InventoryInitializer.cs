using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using tuto.Models;

namespace tuto.DAL
{
    public class InventoryInitializer : System.Data.Entity.DropCreateDatabaseIfModelChanges<InventoryContext>
    {
        protected override void Seed(InventoryContext context)
        {
            var inventories = new List<Inventory>
            {
            new Inventory{BoardName="Carson",NEName="NAB4444",BoardType="Antenne",PN="123",SN="456",ManufacturerData="RRU3539"},
            new Inventory{BoardName="Meredith",NEName="NAB1444",BoardType="Card",PN="156",SN="894",ManufacturerData="RRU3539"},
            new Inventory{BoardName="Arturo",NEName="MED4563",BoardType="card",PN="176",SN="436",ManufacturerData="RRU3539"}
            };

            inventories.ForEach(s => context.Inventories.Add(s));
            context.SaveChanges();
            
        }
    }
}