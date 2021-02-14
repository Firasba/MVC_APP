using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace tuto.Models
{
    public class Inventory
    {


        [Key][DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string BoardName { get; set; }
        public string NEName { get; set; }
        public string BoardType { get; set; }
        public string SN { get; set; }
        public string PN { get; set; }
        public string ManufacturerData { get; set; }

    }
}

