using System.Text.Json.Serialization;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;

namespace TicketSalesAPP.Mobile.UI.MAUI.Domain
{
    [JsonSerializable(typeof(PostResponse))]
    [JsonSerializable(typeof(Author))]
    [JsonSerializable(typeof(bool))]
    public partial class SecuredWebApiTypesContext : JsonSerializerContext { }
}