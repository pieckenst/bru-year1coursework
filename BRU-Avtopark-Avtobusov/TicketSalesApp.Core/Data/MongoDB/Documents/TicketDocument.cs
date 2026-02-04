#if MODERN
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Core.Data.MongoDB.Documents
{
    /// <summary>
    /// MongoDB document representation of Bilet (Ticket) entity
    /// </summary>
    [BsonCollection("tickets")]
    public class TicketDocument : BaseDocument
    {
        [BsonElement("ticketId")]
        public long TicketId { get; set; }
        
        [BsonElement("routeId")]
        public long RouteId { get; set; }
        
        [BsonElement("ticketPrice")]
        public decimal TicketPrice { get; set; }
        
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
        
        [BsonElement("ticketType")]
        public string? TicketType { get; set; }
        
        [BsonElement("validFrom")]
        public DateTime? ValidFrom { get; set; }
        
        [BsonElement("validTo")]
        public DateTime? ValidTo { get; set; }
        
        [BsonElement("routeInfo")]
        public RouteReference? RouteInfo { get; set; }
        
        [BsonElement("sales")]
        public List<SaleReference>? Sales { get; set; }
    }
    
    public class SaleReference
    {
        [BsonElement("saleId")]
        public long SaleId { get; set; }
        
        [BsonElement("saleDate")]
        public DateTime SaleDate { get; set; }
        
        [BsonElement("quantity")]
        public int Quantity { get; set; }
        
        [BsonElement("totalAmount")]
        public decimal TotalAmount { get; set; }
        
        [BsonElement("customerInfo")]
        public string? CustomerInfo { get; set; }
        
        [BsonElement("paymentMethod")]
        public string? PaymentMethod { get; set; }
    }
}
#endif