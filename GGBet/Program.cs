using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GGBet
{
    class Program
    {
        static void Main(string[] args)
        {
            GGBetApi.API a = new GGBetApi.API("https://ggbet.com");
            var c = a.GetLive("counter-strike").Result;
            Console.ReadLine();
        }
    }
}
