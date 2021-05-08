using System;

namespace Skoruba.IdentityServer4.STS.Identity.ViewModels.Manage
{
    public class PhoneTokenTempDataModel
    {
        public string SecretKey { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime SendNextSmsTime { get; set; }
    }
}