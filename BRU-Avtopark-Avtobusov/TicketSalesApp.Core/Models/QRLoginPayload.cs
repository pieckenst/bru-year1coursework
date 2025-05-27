using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#if MODERN
using System.Text.Json.Serialization;
#elif WINDOWSXP
using Newtonsoft.Json;
#endif

namespace TicketSalesApp.Core.Models
{
    public class QRLoginPayload
    {
        public string Name { get; set; } // Will store encrypted username
        public string PersonalAcc { get; set; } // Will store encrypted session ID
        public string BankName { get; set; } // Will store encrypted timestamp
        public string BIC { get; set; } // Will store encrypted validation code
        public string CorrespAcc { get; set; } // Will store encrypted role info
    }
}