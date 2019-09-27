using bGridDataProxy.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace bGridDataProxy.Helpers
{
    public static class BuildingActionHelper
    {


        public static HttpClient GetHttpClient(IOptions<bGrid> bGridConfig)
        {
            var endpoint = bGridConfig.Value.Endpoint;
            var user = bGridConfig.Value.Username;
            var pw = bGridConfig.Value.Password;

            var bGridClient = new HttpClient()
            {
                BaseAddress = new Uri(endpoint),
                Timeout = new TimeSpan(0, 0, 2)
            };


            var byteArray = Encoding.ASCII.GetBytes($"{user}:{pw}");
            bGridClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            return bGridClient;
        }

        public static async Task<T> ExecuteGetAction<T>(string action, IOptions<bGrid> bGridConfig)
        {
            var bGridClient = GetHttpClient(bGridConfig);
            var response = await bGridClient.GetAsync(action);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonObject = JsonConvert.DeserializeObject<T>(jsonString);
                return jsonObject;
            }
            else
            {
                return default(T);
            }
        }
    }
}
