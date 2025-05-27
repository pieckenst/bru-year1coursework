using System.Collections.Generic;

namespace TicketSalesApp.Core.Models
{
    public class Form
    {
        public string FormName { get; set; }
        public List<FormField> Fields { get; set; } = new List<FormField>();
    }

    public class FormField
    {
        public string FieldName { get; set; }
        public string FieldType { get; set; } // e.g., TextBox, ComboBox, etc.
        public bool IsRequired { get; set; }
        public string Placeholder { get; set; }
    }
} 