﻿using MultiMiner.Coin.Api;
using MultiMiner.Coin.Api.Data;
using MultiMiner.CoinWarz.Api.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;

namespace MultiMiner.CoinWarz.Api
{
    public class ApiContext : IApiContext
    {
        private class ApiWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                //default is 100s - far too long for our API calls
                //if API is being flakey we don't want calls taking 100s to timeout
                //lets go with 10s
                w.Timeout = 10 * 1000;
                return w;
            }
        }

        private readonly string apiKey;
        public ApiContext(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public IEnumerable<CoinInformation> GetCoinInformation(string userAgent = "")
        {
            WebClient client = new ApiWebClient();
            if (!string.IsNullOrEmpty(userAgent))
                client.Headers.Add("user-agent", userAgent);

            string apiUrl = GetApiUrl();

            string jsonString = client.DownloadString(apiUrl);

            JObject jsonObject = JObject.Parse(jsonString);
            
            if (!jsonObject.Value<bool>("Success"))
            {
                throw new CoinApiException(jsonObject.Value<string>("Message"));
            }

            JArray jsonArray = jsonObject.Value<JArray>("Data");

            List<CoinInformation> result = new List<CoinInformation>();

            foreach (JToken jToken in jsonArray)
            {
                CoinInformation coinInformation = new CoinInformation();
                coinInformation.PopulateFromJson(jToken);
                if (coinInformation.Difficulty > 0)
                    //only add coins with valid info since the user may be basing
                    //strategies on Difficulty
                    result.Add(coinInformation);
            }

            return result;
        }

        public string GetApiUrl()
        {
            return String.Format(@"http://www.coinwarz.com/v1/api/profitability/?apikey={0}&algo=all", apiKey);
        }

        public string GetInfoUrl()
        {
            return String.Format(@"http://www.coinwarz.com/cryptocurrency", apiKey);
        }

        public string GetApiName()
        {
            return "CoinWarz.com";
        }
    }
}
