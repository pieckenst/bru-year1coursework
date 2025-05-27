namespace TicketSalesAPP.Mobile.UI.MAUI.Domain.Data
{
    public class PostResponse
    {
        public IEnumerable<Post>? Value { get; set; }
    }

    public class Post
    {
        public int PostId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public Author? Author { get; set; }
    }
}