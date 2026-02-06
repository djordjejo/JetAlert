using JetAlert.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetAlert.Services
{
    public class ScraperService
    {
        private readonly IWebDriver _driver;
        private readonly IConfiguration _configuration;

        public ScraperService(IConfiguration configuration)
        {
            _configuration = configuration;

            var options = new ChromeOptions();
            options.AddArgument("--headless");              // Radi bez prozora
            options.AddArgument("--disable-gpu");           // Isključi GPU rendering
            options.AddArgument("--no-sandbox");            // Za Docker/Linux
            options.AddArgument("--disable-dev-shm-usage"); // Manje memorije

            var userAgent = _configuration["ScraperSettings:UserAgent"]
                 ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
            options.AddArgument($"user-agent={userAgent}");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }
        public async Task<List<Flight>> ScrapeFlightsAsync(string origin, string destination, DateTime departure_date, DateTime returnDate, int adults)
        { 
            var flights = new List<Flight>();

            var url = $"https://booking.airserbia.com/dx/JUDX/#/flight-selection" +
              $"?journeyType=round-trip" +
              $"&origin={origin}" +
              $"&destination={destination}" +
              $"&date={departure_date:MM-dd-yyyy}" +
              $"&origin1={destination}" +
              $"&destination1={origin}" +
              $"&date1={returnDate:MM-dd-yyyy}" +
              $"&ADT={adults}" +
              $"&C13=0&YTH=0&CHD=0&INF=0" +
              $"&pointOfSale=RS" +
              $"&locale=sr-LATN";

            try
            {
                _driver.Navigate().GoToUrl(url);

                var delay = _configuration.GetValue<int>("ScraperSettings:DelayBetweenRequests", 5000);
                await Task.Delay(delay); // ovde kazem programu da saceka 5s jer je potrebno vreme da se otvori web stranica

                var flightCards = _driver.FindElements(By.XPath("//*[@id='dxp-flight-table-section']/div[2]/div/div"));

                if (flightCards == null || !flightCards.Any())
                {
                    Console.WriteLine("  ⚠️  No flights found. Check selectors!");
                    return flights;
                }
                
                foreach (var card in flightCards)
                {
                    try
                    {
                        var detailsBtn = card.FindElement(By.XPath("//*[@id=\"dxp-flight-table-section\"]/div[2]/div/div[2]/div[1]/button[1]/span[1]"));
                        var economyPrice = detailsBtn.FindElement(By.XPath("//*[@id=\"dxp-flight-table-section\"]/div[2]/div/div[1]/div[2]/div[1]/div/button/div/div[2]/div/span/span/span/span/span/span[2]/span"));
                        var sxp_time = card.FindElement(By.XPath("//*[@id=\"dxp-flight-table-section\"]/div[2]/div/div[1]/div[1]/div/div[1]/div[1]/div[1]/div[1]/time"));
                        var dxp_time = card.FindElement(By.XPath("//*[@id=\"dxp-flight-table-section\"]/div[2]/div/div[1]/div[1]/div/div[1]/div[1]/div[2]/div[1]/time"));
                        await Task.Delay(1000);

                        var extraInfo = card.FindElement(By.XPath("//*[@id=\"dxp-flight-table-section\"]/div[2]/div/div[2]/div[2]/div/div/div"));

                        var duration = card.FindElement(By.XPath("//*[@id=\"dxp-flight-table-section\"]/div[2]/div/div[1]/div[1]/div/div[1]/div[2]/div[1]/time/span"));
                        //var duration = extraInfo.FindElement(By.XPath(""));
                        //var duration = extraInfo.FindElement(By.XPath(""));
                        //var duration = extraInfo.FindElement(By.XPath(""));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsing flight node: " + ex);
                    }
                    finally
                    { 
                        
                    }
                }

            }
            catch (Exception ex)
            {
               Console.WriteLine("Greska: " + ex);
            }
        }
    }
}
