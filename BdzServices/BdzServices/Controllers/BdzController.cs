namespace BdzServices.Controllers
{
    using BdzServices.Models;
    using HtmlAgilityPack;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    public class BdzController : ApiController
    {
        [HttpGet]
        public async Task<IHttpActionResult> GetRouteInfo(string from, string to, string date)
        {
            var url = "http://razpisanie.bdz.bg/SearchServlet?action=listOptions";

            var client = new HttpClient();

            string a = "\\\"Златни Пясъци\\\"";
            bool s = a.StartsWith("\\\"");
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Post;

            var args = new List<KeyValuePair<string, string>>();
            args.Add(new KeyValuePair<string, string>("from_station", from));
            args.Add(new KeyValuePair<string, string>("to_station", to));
            args.Add(new KeyValuePair<string, string>("date", date));
            args.Add(new KeyValuePair<string, string>("dep_arr", "1"));
            args.Add(new KeyValuePair<string, string>("time_from", "00:00"));
            args.Add(new KeyValuePair<string, string>("time_to", "24:00"));
            args.Add(new KeyValuePair<string, string>("all_cats", ""));
            args.Add(new KeyValuePair<string, string>("sort_by", "0"));
            args.Add(new KeyValuePair<string, string>("lang", ""));
            request.Content = new FormUrlEncodedContent(args);

            var response = await client.SendAsync(request);
            var responseStream = await response.Content.ReadAsStreamAsync();
            using (StreamReader reader = new StreamReader(responseStream))
            {
                var html = reader.ReadToEnd();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var detailedInformation = htmlDoc.DocumentNode.SelectNodes("//div[@class=\"cont\"]");
                var basicInformation = htmlDoc.DocumentNode.SelectNodes("//div[@class=\"accordionTabTitleBar\"]"); //returns basic info table


                var details = detailedInformation.Select(node => node.InnerText.Trim()
                   .Replace("\r\n", string.Empty).Split(new string[] { "  " }, StringSplitOptions.RemoveEmptyEntries).ToList()).ToList();
                var partsToRemove = new List<string> {"Влак",
                                                       "Гара/Спирка",
                                                       "Състав",
                                                       "Заминава",
                                                       "Пристига",
                                                       "&raquo;",
                                                       "Вариант",
                                                       "за",
                                                       "печат",
                                                       "Карта",
                                                       "на",
                                                       "маршрута",
                                                       "Цени",
                                                       " Вариант за печат",
                                                       "Карта на маршрута",
                                                       " Цени",
                                                       "Вариант за печат",
                                                       "|"};
                foreach (var entry in details)
                {
                    removeExtraParts(partsToRemove, entry);
                }
                var basic = basicInformation.Select(node => node.InnerText.Replace(" ", string.Empty).Trim().Replace("\r\n", ",").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)).ToList();
                var result = this.ParseRouteInfo(basic, details);
                return Ok(result);
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetStationInfo(string station, string date)
        {
            var url = "http://razpisanie.bdz.bg/SearchServlet?action=listStation";

            var client = new HttpClient();

            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Post;

            var args = new List<KeyValuePair<string, string>>();

            args.Add(new KeyValuePair<string, string>("station", station));
            args.Add(new KeyValuePair<string, string>("date", date));
            args.Add(new KeyValuePair<string, string>("lang", "bg"));
            request.Content = new FormUrlEncodedContent(args);

            var response = await client.SendAsync(request);
            var responseStream = await response.Content.ReadAsStreamAsync();
            using (StreamReader reader = new StreamReader(responseStream))
            {
                var html = reader.ReadToEnd();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);
                var detailedInformation = htmlDoc.DocumentNode.SelectNodes("//table[@class=\"info_table\"]");
                var parsed = detailedInformation.First().InnerText.Replace("\r\n", string.Empty);
                var parts = parsed.Split(new string[] { "  " }, StringSplitOptions.RemoveEmptyEntries).ToList();
                var partsToRemove = new List<string> {"&nbsp;",
                                                  "От гара/спирка",
                                                  "Влак",
                                                  "Коментар",
                                                  "Заминава",
                                                   " <!--(Empty Arr List)-->"};

                removeExtraParts(partsToRemove, parts);
                var indexOfList = parts.IndexOf("<!--Error:: Empty Dep List-->");
                for (int i = 0; i < indexOfList + 1; i++)
                {
                    parts.RemoveAt(0);
                }

                parts.Remove("Пристига");
                var result = this.ParseTrains(parts);
                return Ok(result);
            }

        }

        private void removeExtraParts(IList<string> extras, List<string> removeFrom)
        {
            removeFrom.RemoveAll(row => extras.IndexOf(row) >= 0 || row.StartsWith("\""));
        }

        private IDictionary<string, IList<Route>> ParseRouteInfo(IList<string[]> basic, IList<List<string>> detailed)
        {
            IDictionary<string, IList<Route>> result = new Dictionary<string, IList<Route>>();
            IList<Route> possibleRoutes = new List<Route>();
            for (int i = 0; i < basic.Count; i++)
            {
                var variant = basic[i];
                possibleRoutes.Add(new Route(variant[1], variant[2], int.Parse(variant[4]), variant[5], detailed[i]));

            }
            result.Add("Routes", possibleRoutes);
            return result;
        }

        private IDictionary<string, IList<Train>> ParseTrains(IList<string> parts)
        {
            IDictionary<string, IList<Train>> result = new Dictionary<string, IList<Train>>();
            IList<Train> departuringTrains = new List<Train>();
            IList<Train> arrivingTrains = new List<Train>();

            int startingArrivals = parts.IndexOf("Пристига");
            for (int i = 0; i < startingArrivals; i += 3)
            {
                departuringTrains.Add(new Train(parts[i], parts[i + 1], parts[i + 2]));
            }

            for (int i = startingArrivals + 1; i < parts.Count - 2; i += 3)
            {
                arrivingTrains.Add(new Train(parts[i], parts[i + 1], parts[i + 2]));
            }

            result.Add("Departure", departuringTrains);
            result.Add("Arrival", arrivingTrains);
            return result;
        }
    }
}
