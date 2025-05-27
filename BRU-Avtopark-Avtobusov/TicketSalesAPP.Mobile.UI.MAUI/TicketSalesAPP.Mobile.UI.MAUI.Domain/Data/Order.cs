using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSalesAPP.Mobile.UI.MAUI.Domain.Data
{
    public enum OrderState
    {
        Draft,
        Shipping,
        Paid,
        Processed
    }

    public class Order
    {
        public int Id { get; set; }
        public OrderState State { get; set; }
        public string? Comment { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal Price { get; set; }

        [NotMapped]
        public bool IsPaid { get => (State != OrderState.Paid) && (State != OrderState.Processed); }

        public override int GetHashCode()
        {
            return Id;
        }
        public override bool Equals(object? obj)
        {
            if (obj is not Order order)
                return false;
            return Id == order.Id;
        }
        public static void Copy(Order source, Order target)
        {
            target.Id = source.Id;
            target.State = source.State;
            target.Comment = source.Comment;
            target.OrderDate = source.OrderDate;
            target.Price = source.Price;
        }
    }
}