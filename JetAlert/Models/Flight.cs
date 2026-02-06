using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetAlert.Model
{
    public class Flight
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Duration{ get; set; } = string.Empty;
        public string Seats { get; set; }
        public string Url { get; set; } = string.Empty;
        public DateTime ScrapedAt { get; set; }

        public List<PriceHistory> PriceHistory { get; set; } = new();
    }

}
