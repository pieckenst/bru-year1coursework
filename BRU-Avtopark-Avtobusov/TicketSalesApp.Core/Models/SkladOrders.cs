using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if MODERN
using System.Text.Json.Serialization;
#elif WINDOWSXP
using Newtonsoft.Json;
#endif

namespace TicketSalesApp.Core.Models
{
    internal class IncomingOrders
    {
        [Key]
        public long Incomingorderid { get; set; }

        public long linktoskladorderid { get; set; }

#if MODERN
        public string? ToverName { get; set; }

        public string? tovarbarcode { get; set; }
#else
        public string ToverName { get; set; }

        public string tovarbarcode { get; set; }
#endif
    }

    internal class OutgoingOrders
    {

        [Key]
        public long Outgoingorderid { get; set; }

        public long linktoskladorderid { get; set; }

#if MODERN
        public string? ToverName { get; set; }

        public string? tovarbarcode { get; set; }
#else
        public string ToverName { get; set; }

        public string tovarbarcode { get; set; }
#endif

    }

    internal class SkladOrders
    {

        [Key]
        public long Skladorderid { get; set; }

#if MODERN
        public string? Name { get; set; }

        public string? Quantity { get; set; }

        public string? Address { get; set; }

        public string? Fax { get; set; }

        public string? Zip { get; set; }

        public string? Remark { get; set; }
#else
        public string Name { get; set; }

        public string Quantity { get; set; }

        public string Address { get; set; }

        public string Fax { get; set; }

        public string Zip { get; set; }

        public string Remark { get; set; }
#endif

    }
}
