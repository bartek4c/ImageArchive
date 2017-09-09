using ImageArchive.Services.Handlers;
using ImageArchive.Services.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ImageArchive.Services
{
    public class AzureService : IAzureService
    {
        public bool VerifyDbFirewallRules(string armResource, string tokenEndpoint, string spnPayload, string clientId, string tenantId, string clientSecret, string armUrl)
        {
            //check current ip address
            string url = "http://checkip.dyndns.org";
            System.Net.WebRequest req = System.Net.WebRequest.Create(url);
            System.Net.WebResponse resp = req.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
            string response = sr.ReadToEnd().Trim();
            string[] a = response.Split(':');
            string a2 = a[1].Substring(1);
            string[] a3 = a2.Split('<');
            string currentIpAddress = a3[0];

            //get authorization token to call ARM API
            string token = AcquireTokenBySPN(tenantId, clientId, clientSecret, spnPayload, armResource, tokenEndpoint).Result;
            //get IP associated with "home" rule
            string ruleIpAddress = GetFirewallIp(token, armUrl).Result;

            //if addresses are different update Azure db home firewall rule IP address
            if (currentIpAddress.Trim() != ruleIpAddress.Trim())
            {
                return UpdateFirewallIp(token, currentIpAddress, armUrl).Result;
            }
            return true;
        }

        private async Task<bool> UpdateFirewallIp(string token, string myIpAddress, string armUrl)
        {
            var success = false;

            var properties = new
            {
                properties = new
                {
                    startIpAddress = myIpAddress,
                    endIpAddress = myIpAddress
                }
            };

            using (var client = new HttpClient(new LoggingHandler(new HttpClientHandler())))
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                client.BaseAddress = new Uri("https://management.azure.com/");

                var json = JsonConvert.SerializeObject(properties, Formatting.Indented);

                var method = new HttpMethod("PUT");
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(method, armUrl) { Content = content };
                HttpResponseMessage putResponse = await client.SendAsync(request);
                //clarify the request was successfull
                success = (putResponse.StatusCode == HttpStatusCode.OK);
            }

            return success;
        }

        private async Task<string> GetFirewallIp(string token, string armUrl)
        {
            var ip = "";
            using (var client = new HttpClient(new LoggingHandler(new HttpClientHandler())))
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                client.BaseAddress = new Uri("https://management.azure.com/");

                var response = await client.GetAsync(armUrl);
                response.EnsureSuccessStatusCode();

                var rule = JsonConvert.DeserializeObject<FirewallRule>(await response.Content.ReadAsStringAsync());
                if (rule.Properties.StartIpAddress == rule.Properties.EndIpAddress)
                {
                    ip = rule.Properties.StartIpAddress;
                }
            }
            return ip;
        }

        private async Task<string> AcquireTokenBySPN(string tenantId, string clientId, string clientSecret, string spnPayload, string armResource, string tokenEndpoint)
        {
            var payload = String.Format(spnPayload,
                                    WebUtility.UrlEncode(armResource),
                                    WebUtility.UrlEncode(clientId),
                                    WebUtility.UrlEncode(clientSecret));

            var body = await HttpPost(tenantId, payload, tokenEndpoint);
            return body.access_token;
        }

        private async Task<dynamic> HttpPost(string tenantId, string payload, string tokenEndpoint)
        {
            using (var client = new HttpClient())
            {
                var address = String.Format(tokenEndpoint, tenantId);
                var content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded");
                using (var response = await client.PostAsync(address, content))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsAsync<dynamic>();
                }
            }
        }
    }
}
