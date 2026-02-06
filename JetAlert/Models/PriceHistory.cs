using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetAlert.Model
{
    public class PriceHistory
    {
        public int Id { get; set; }
        public int FlightId { get; set; }
        public decimal Price { get; set; }
        public DateTime RecordedAt { get; set; }
        public Flight Flight { get; set; } = null!;
    }
}
