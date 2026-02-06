using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using JetAlert.Data;
using JetAlert.Services;
using JetAlert.Model; 

namespace JetAlert;  

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection");

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString));

                services.AddScoped<ScraperService>();
                services.AddScoped<NotificationService>();
            })
            .Build();

        Console.WriteLine("✈️  JetAlert - Flight Tracker");
        Console.WriteLine("═══════════════════════════════════════════\n");

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }

        await RunScrapingAsync(host.Services);
    }

    // DODAJ OVU METODU ISPOD Main():
    static async Task RunScrapingAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var scraper = scope.ServiceProvider.GetRequiredService<ScraperService>();
        var notifier = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            var routes = new[]
            {
                ("BEG", "NYC", new DateTime(2026, 6, 15)),
                ("BEG", "PAR", new DateTime(2026, 7, 1)),
            };

            foreach (var (origin, dest, date) in routes)
            {
                Console.WriteLine($"Scraping: {origin} → {dest} on {date:dd.MM.yyyy}");

                var flights = await scraper.ScrapeFlightsAsync(origin, dest, date);

                Console.WriteLine($"Found {flights.Count} flights\n");

                foreach (var flight in flights)
                {
                    var existing = await db.Flights
                        .Include(f => f.PriceHistory)
                        .FirstOrDefaultAsync(f =>
                            f.FlightNumber == flight.FlightNumber &&
                            f.DepartureDate.Date == flight.DepartureDate.Date);

                    if (existing != null)
                    {
                        var lastPrice = existing.PriceHistory
                            .OrderByDescending(p => p.RecordedAt)
                            .FirstOrDefault()?.Price ?? existing.Price;

                        if (Math.Abs(lastPrice - flight.Price) > 0.01m)
                        {
                            await notifier.SendPriceAlertAsync(flight, lastPrice, flight.Price);

                            existing.PriceHistory.Add(new JetAlert.Models.PriceHistory
                            {
                                Price = flight.Price,
                                RecordedAt = DateTime.UtcNow
                            });

                            existing.Price = flight.Price;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  ✓ {flight.FlightNumber}: {flight.Price:N0} {flight.Currency}");

                        flight.PriceHistory.Add(new JetAlert.Models.PriceHistory
                        {
                            Price = flight.Price,
                            RecordedAt = DateTime.UtcNow
                        });

                        await db.Flights.AddAsync(flight);
                    }
                }

                await db.SaveChangesAsync();
            }

            var allFlights = await db.Flights.ToListAsync();
            await notifier.SendDailySummaryAsync(allFlights);
        }
        finally
        {
            scraper.Dispose();
        }

        Console.WriteLine("\n✅ Scraping completed!");
    }
}
