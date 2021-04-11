using System.Threading.Tasks;

namespace Skoruba.IdentityServer4.STS.Identity.Services.SmsService
{
    public interface ISmsSender
    {
        public Task<bool> SendSmsAsync(string mobileNumber, string message);
    }
}