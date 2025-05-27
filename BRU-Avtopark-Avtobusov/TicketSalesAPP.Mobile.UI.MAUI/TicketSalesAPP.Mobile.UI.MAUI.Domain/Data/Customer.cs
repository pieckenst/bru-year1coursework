namespace TicketSalesAPP.Mobile.UI.MAUI.Domain.Data
{
    public class Customer
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Company { get; set; }
        public string? Email { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        public Customer() { }
        public Customer(int id)
        {
            Id = id;
            FirstName = $"FirstName{id}";
            LastName = $"LastName{id}";
            Company = $"Company{id}";
            Email = $"{FirstName}.{LastName}@{Company}.com";
        }

        public override int GetHashCode()
        {
            return Id;
        }
        public override bool Equals(object? obj)
        {
            if (obj is not Customer customer)
                return false;
            return Id == customer.Id;
        }
        public static void Copy(Customer source, Customer target)
        {
            target.Id = source.Id;
            target.FirstName = source.FirstName;
            target.LastName = source.LastName;
            target.Company = source.Company;
            target.Email = source.Email;
        }
    }
}