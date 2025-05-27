using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Models // Adjusted namespace
{
    public class QRLoginPayload
    {
        public string Name { get; set; } // Will store encrypted username // Non-nullable
        public string PersonalAcc { get; set; } // Will store encrypted session ID // Non-nullable
        public string BankName { get; set; } // Will store encrypted timestamp // Non-nullable
        public string BIC { get; set; } // Will store encrypted validation code // Non-nullable
        public string CorrespAcc { get; set; } // Will store encrypted role info // Non-nullable
    }
} 