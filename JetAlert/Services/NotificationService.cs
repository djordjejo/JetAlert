using JetAlert.Model;
using OpenQA.Selenium.DevTools.V129.Emulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace JetAlert.Services
{
    public class NotificationService
    {
        public async Task SendPriceAlertAsync(Flight flight, decimal oldPrice, decimal newPrice)
        {
            var priceChange = ((newPrice - oldPrice))/ oldPrice * 100;
            var changeEmoji = priceChange < 0 ? "📉" : "📈";

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine($"{changeEmoji} PRICE ALERT!");
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine($"Flight:      {flight.FlightNumber}");
            Console.WriteLine($"Route:       {flight.Origin} → {flight.Destination}");
            Console.WriteLine($"Date:        {flight.DepartureDate:dd.MM.yyyy}");
            Console.WriteLine($"Old Price:   {oldPrice:N0} {flight.Currency}");
            Console.WriteLine($"New Price:   {newPrice:N0} {flight.Currency}");
            Console.WriteLine($"Change:      {priceChange:+0.00;-0.00}%");
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine();

            // IMPLEMENTIRAJ SendEmailAsync
        }
        public async Task SendDailySummaryAsync(List<Flight>flights)
        {
            if (!flights.Any())
            { 
                Console.WriteLine("No flights");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine("📊 DAILY SUMMARY");
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine($"Tracked Flights: {flights.Count}");

            var cheapest = flights.MinBy(f => f.Price);
            var expensive = flights.MaxBy(f => f.Price);
            var avgPrice = flights.Average(f => f.Price);

            Console.WriteLine($"Lowest Price:    {cheapest?.Price:N0} RSD ({cheapest?.FlightNumber})");
            Console.WriteLine($"Highest Price:   {expensive?.Price:N0} RSD ({expensive?.FlightNumber})");
            Console.WriteLine($"Average Price:   {avgPrice:N0} RSD");
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine();

            await Task.CompletedTask;
        }
        private async Task SendEmailAsync(Flight flight, decimal oldPrice, decimal newPrice)
        {
            // Koristi MailKit ili SendGrid
            await Task.CompletedTask;
        }
    }
}
