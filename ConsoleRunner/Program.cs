using System.Configuration;
using System;
using Core;
using Scrapers;

namespace ConsoleRunner
{
    class Program
    {
        public static void Main(string[] args)
        {
            //scrape
            //Onlinefun.Scrape();

            var imgurClientId = ConfigurationManager.AppSettings["imgurClientId"];
            var imgurClientSecret = ConfigurationManager.AppSettings["imgurClientSecret"];

            var azureKey = ConfigurationManager.AppSettings["azureKey"];
            var azureRegion = ConfigurationManager.AppSettings["azureRegion"];

            var punsPath = ConfigurationManager.AppSettings["punPath"];

            var analyzer = new Analyzer(imgurClientId, imgurClientSecret, azureKey, azureRegion, new DataRepository(punsPath));

            analyzer.Run();
            
            Console.ReadLine();
        }
    }
}
