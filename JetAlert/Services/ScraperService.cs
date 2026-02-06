using JetAlert.Model;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace JetAlert.Services
{
    /*https://booking.airserbia.com/dx/JUDX/#/matrix?journeyType=round-trip&pointOfSale=RS&locale=sr-LATN&awardBooking=false&searchType=BRANDED&ADT=2&C13=0&YTH=0&SRC=0&CHD=0&INF=0
     * &origin=BEG&destination=VIE&date=06-15-2026&origin1=VIE&destination1=BEG&date1=06-22-2026&direction=0*/
    public class ScraperService : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly IConfiguration _configuration;

        public ScraperService(IConfiguration configuration)
        {
            _configuration = configuration;

            var options = new ChromeOptions();
            //  options.AddArgument("--headless");
            options.BinaryLocation = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";          
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");

            var userAgent = _configuration["ScraperSettings:UserAgent"]
                 ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
            options.AddArgument($"user-agent={userAgent}");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        public async Task<List<Flight>> ScrapeMatrixAsync(
     string origin,
     string destination,
     DateTime departureDate,
     DateTime returnDate,
     int adults)
        {
            var allFlights = new List<Flight>();

            var url = $"https://booking.airserbia.com/dx/JUDX/#/matrix" +
                $"?journeyType=round-trip" +
                $"&origin={origin}&destination={destination}" +
                $"&date={departureDate:MM-dd-yyyy}" +
                $"&origin1={destination}&destination1={origin}" +
                $"&date1={returnDate:MM-dd-yyyy}" +
                $"&ADT={adults}" +
                $"&pointOfSale=RS&locale=sr-LATN";

            _driver.Navigate().GoToUrl(url);
            await Task.Delay(8000);

            // Nađi SVE price buttons u matrix-u
            var priceCells = _driver.FindElements(By.CssSelector("button.dxp-matrix-grid-cell-new"));
            Console.WriteLine($"Found {priceCells.Count} price combinations");

            for (int i = 0; i < Math.Min(priceCells.Count, 10); i++)
            {
                try
                {
                    var cells = _driver.FindElements(By.CssSelector("button.dxp-matrix-grid-cell-new"));

                    // Scroll + klik na matrix cell
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", cells[i]);
                    await Task.Delay(500);
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", cells[i]);
                    await Task.Delay(2000);

                    var continueBtn = _driver.FindElement(By.CssSelector("button.dxp-matrix-footer-search-button"));
                    continueBtn.Click();
                    await Task.Delay(10000);  // ← Povećaj

                    // DEBUG
                    Console.WriteLine($"URL after continue: {_driver.Url}");
                    var html = _driver.PageSource;
                    Console.WriteLine($"HTML contains 'dxp-itinerary-part-offer': {html.Contains("dxp-itinerary-part-offer")}");

                    var flights = await ScrapeCurrentPageFlightsAsync(origin, destination, departureDate);
                    allFlights.AddRange(flights);

                    Console.WriteLine($"Combination {i + 1}: Found {flights.Count} flights");

                    // Back na matrix
                    _driver.Navigate().Back();
                    await Task.Delay(3000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error {i}: {ex.Message}");
                }
            }

            return allFlights;
        }

        // HELPER metoda - scrape trenutne stranice
        private async Task<List<Flight>> ScrapeCurrentPageFlightsAsync(
            string origin,
            string destination,
            DateTime departureDate)
        {
            var flights = new List<Flight>();

            try
            {
                var delay = _configuration.GetValue<int>("ScraperSettings:DelayBetweenRequests", 2000);
                await Task.Delay(delay);

                var flightCards = _driver.FindElements(By.CssSelector("div.dxp-itinerary-part-offer"));

                Console.WriteLine($"  Found {flightCards.Count} flight cards on page");

                if (!flightCards.Any())
                    return flights;

                for (int i = 0; i < flightCards.Count; i++)
                {
                    try
                    {
                        var cards = _driver.FindElements(By.CssSelector("div.dxp-itinerary-part-offer"));
                        var card = cards[i];

                        var price = card.FindElement(By.CssSelector("span.number"));
                        var flightNum = card.FindElement(By.CssSelector("div.flight-number"));
                        var duration = card.FindElement(By.CssSelector("time.dxp-duration span"));
                        var seats = card.FindElement(By.CssSelector("div.itinerary-part-remaining-seats span"));

                        var flight = new Flight
                        {
                            FlightNumber = flightNum.Text.Trim(),
                            Origin = origin,
                            Destination = destination,
                            DepartureDate = departureDate,
                            Price = ParsePrice(price.Text),
                            Seats = seats.Text.Trim(),
                            Duration = duration.Text.Trim(),
                            Currency = "EUR",
                            ScrapedAt = DateTime.UtcNow
                        };

                        flights.Add(flight);
                        Console.WriteLine($"    ✓ {flight.FlightNumber}: {flight.Price} EUR");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Error flight {i}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping page: {ex.Message}");
            }

            return flights;
        }

        private decimal ParsePrice(string priceText)
        {
            var cleaned = new string(priceText.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            cleaned = cleaned.Replace(',', '.');
            return decimal.TryParse(cleaned, out var price) ? price : 0;
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}