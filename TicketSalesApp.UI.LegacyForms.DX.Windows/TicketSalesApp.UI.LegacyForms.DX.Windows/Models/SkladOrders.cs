using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks; // Tasks might require Microsoft.Bcl.Async for net40
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Models // Adjusted namespace
{
    internal class IncomingOrders // Kept internal
    {
        [Key]
        public long Incomingorderid { get; set; }

        public long linktoskladorderid { get; set; }

        public string ToverName { get; set; } // Non-nullable

        public string tovarbarcode { get; set; } // Non-nullable
    }

    internal class OutgoingOrders // Kept internal
    {
        [Key]
        public long Outgoingorderid { get; set; }

        public long linktoskladorderid { get; set; }

        public string ToverName { get; set; } // Non-nullable

        public string tovarbarcode { get; set; } // Non-nullable
    }

    internal class SkladOrders // Kept internal
    {
        [Key]
        public long Skladorderid { get; set; }

        public string Name { get; set; } // Non-nullable

        public string Quantity { get; set; } // Non-nullable

        public string Address { get; set; } // Non-nullable

        public string Fax { get; set; } // Non-nullable

        public string Zip { get; set; } // Non-nullable

        public string Remark { get; set; } // Non-nullable
    }
} 