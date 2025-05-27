using System.Text.Json.Serialization;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;

namespace TicketSalesAPP.Mobile.UI.MAUI.Domain
{
    [JsonSerializable(typeof(IEnumerable<Order>))]
    public partial class WebApiTypesContext : JsonSerializerContext { }
}