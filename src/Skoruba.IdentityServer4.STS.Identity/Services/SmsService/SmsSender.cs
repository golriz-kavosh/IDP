using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Skoruba.IdentityServer4.STS.Identity.Configuration;

namespace Skoruba.IdentityServer4.STS.Identity.Services.SmsService
{
    public class SmsSender:ISmsSender
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly SmsOptions _options;

        public SmsSender(IOptions<SmsOptions> options)
            => _options = options?.Value ?? new SmsOptions();
        
        public async Task<bool> SendSmsAsync(string mobileNumber, string message)
        {
            var body = JsonConvert.SerializeObject(
                new
                {
                    Number = mobileNumber,
                    Message = message
                });

            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response =await _client.PostAsync(_options.UrlAddress + "/send", content);

            var responseString =await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode;
        }
    }
}
