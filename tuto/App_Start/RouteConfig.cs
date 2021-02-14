using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace tuto
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
     

            routes.MapRoute(
                name: "Recherche",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Inventory", action = "Search", id = UrlParameter.Optional }
            );
            routes.MapRoute(
        name: "Recherche2",
                url: "{controller}/{action}/{id1}/{id2}/{id3}/{id4}/{id5}",
                defaults: new { controller = "Inventory", action = "Search2", id1 ="",id2="", id3 ="", id4 ="", id5 =""}
            );
            routes.MapRoute(
         name: "ImportFile",
         url: "{controller}/{action}/{id}",
         defaults: new { controller = "Inventory", action = "Indexxx", id = UrlParameter.Optional }
               );
            routes.MapRoute(
                name: "InventoryDataList", 
                url: "{controller}/{action}/{id}", 
                defaults: new { controller = "Inventory", action = "InventoryList" });
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
            
        }
    }
}
