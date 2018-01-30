using System.Configuration;
using System;
using Core;

namespace ConsoleRunner
{
    class Program
    {
        public static void Main(string[] args)
        {
            var imgurClientId = ConfigurationManager.AppSettings["imgurClientId"];
            var imgurClientSecret = ConfigurationManager.AppSettings["imgurClientSecret"];

            var azureKey = ConfigurationManager.AppSettings["azureKey"];
            var azureRegion = ConfigurationManager.AppSettings["azureRegion"];

            var analyzer = new Analyzer(imgurClientId, imgurClientSecret, azureKey, azureRegion);

            analyzer.Run();

            Console.ReadLine();
        }
    }
}
