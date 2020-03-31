using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace GGBet.GGBetApi
{
    class API
    {
        private readonly string BaseUrl = "";
        private string Status;
        private List<GGBetEvent> Events = new List<GGBetEvent> { };
        private Browser Browser = null;
        private Page Page = null;


        public API(string mirrorUrl)
        {
            BaseUrl = mirrorUrl;
            return;
        }

        ~API()
        {
            Page?.CloseAsync().Wait();
            Browser?.CloseAsync().Wait();
        }

        async public Task<List<GGBetEvent>> GetLive(string discipline)
        {
            GetAsync(discipline, true);
            return await GetEvents();
        }


        async public Task<List<GGBetEvent>> GetLine(string discipline)
        {
            GetAsync(discipline, false);
            return await GetEvents();
        }

        async private void GetAsync(string discipline, bool isLive, string dateFrom = null, string dateTo = null)
        {
            if (!isLive) Status = "NOT_STARTED";
            else Status = "LIVE";
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            string url = GenerateUrl(discipline, dateFrom, dateTo);

            var browserData = await CreateBrowserAndPageAsync();
            var browser = browserData.Item1;
            var page = browserData.Item2;
            Browser = browser;
            Page = page;
            await page.GoToAsync(url);
            var f12 = await page.Target.CreateCDPSessionAsync();
            await f12.SendAsync("Network.enable");
            await f12.SendAsync("Page.enable");


            f12.MessageReceived += handleWebSoket;

        }

        private bool FindMatches = false;
        async private Task<List<GGBetEvent>> GetEvents()
        {
            while (true)
            {
                if (FindMatches)
                {
                    FindMatches = false;
                    break;
                }
            }
            
            await Browser?.CloseAsync();
            await Page?.CloseAsync();

            return Events;
        }

        void handleWebSoket(object sender, MessageEventArgs args)
        {
            if (args.MessageID != "Network.webSocketFrameReceived") return;
            var json = JsonConvert.DeserializeObject(args.MessageData.ToString()) as JObject;
            if (json != null)
            {

                try
                {
                    json = JsonConvert.DeserializeObject(json["response"]["payloadData"].ToString()) as JObject;
                }
                catch
                {
                    json = null;
                }
                if (json == null) return;


                if (json.Properties().Select(p => p.Name).ToList().Contains("payload"))
                {
                    bool hasMatches = false;
                    try
                    {
                        if ((json["payload"]["data"] as JObject).Properties().Select(p => p.Name).ToList().Contains("matches")) hasMatches = true;
                    }
                    catch
                    {
                        hasMatches=false;
                    }

                    if (hasMatches)
                    {
                        foreach (var match in json["payload"]["data"]["matches"])
                        {
                            var c = match["fixture"]["status"].ToString();
                            if (match["fixture"]["status"].ToString() != Status) continue;

                            Dictionary<string, dynamic> oddList = new Dictionary<string, dynamic>();

                            foreach (var market in match["markets"])
                            {
                                var name = market["name"].ToString();
                                foreach (var odd in market["odds"])
                                {
                                    oddList.Add(name + " " + odd["name"], odd["value"]);
                                }
                            }

                            Events.Add(new GGBetEvent
                            {
                                EventID = match["id"].ToString(),
                                LeagueName = match["fixture"]["tournament"]["name"].ToString(),
                                TeamString = match["fixture"]["title"].ToString(),
                                Teams = new GGBetEvent._Teams
                                {
                                    Team1 = match["fixture"]["competitors"][0]["name"].ToString(),
                                    Team2 = match["fixture"]["competitors"][1]["name"].ToString(),
                                },
                                OddList = oddList
                            });
                        }

                        FindMatches = true;
                        
                    }
                }
            }

        }

        async private Task<Tuple<Browser, Page>> CreateBrowserAndPageAsync()
        {

            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });
            var page = await browser.NewPageAsync();
            await page.SetUserAgentAsync("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.117 Safari/537.36");
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 1920,
                Height = 1080
            });

            return Tuple.Create(browser, page);
        }

        private string GenerateUrl(string discipline, string dateFrom, string dateTo, int urlPage = 1)
        {
            return $"{BaseUrl}/en/{discipline}?page={urlPage}{GenerateDateFromUrl(dateFrom, dateTo)}";
        }

        private string GenerateDateFromUrl(string dateFrom, string dateTo)
        {
            if (dateFrom == null && dateTo == null)
                return "";
            else
            {
                string dateFromUrlString = "";
                if (dateFrom != null)
                {

                    string month = DateTime.Parse(dateFrom).Month.ToString();
                    if (month.Length == 1) month = "0" + month;
                    string day = DateTime.Parse(dateFrom).Day.ToString();
                    if (day.Length == 1) day = "0" + day;
                    string year = DateTime.Parse(dateFrom).Year.ToString();
                    dateFromUrlString += $"&dateFrom={year}-{month}-{day}";
                }
                if (dateTo != null)
                {
                    string month = DateTime.Parse(dateTo).Month.ToString();
                    if (month.Length == 1) month = "0" + month;
                    string day = DateTime.Parse(dateTo).Day.ToString();
                    if (day.Length == 1) day = "0" + day;
                    string year = DateTime.Parse(dateTo).Year.ToString();
                    dateFromUrlString += $"&dateTo={year}-{month}-{day}";
                }
                return dateFromUrlString;
            }
        }
    }
}
