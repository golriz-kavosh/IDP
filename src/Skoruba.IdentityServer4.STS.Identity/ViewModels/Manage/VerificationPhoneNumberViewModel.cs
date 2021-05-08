using System.ComponentModel.DataAnnotations;

namespace Skoruba.IdentityServer4.STS.Identity.ViewModels.Manage
{
    public class VerificationPhoneNumberViewModel
    {
        [Required]
        [StringLength(6)]
        [Display(Name = "Received code")]
        [RegularExpression("^\\d{6}", ErrorMessage = "Invalid Code")]
        public string Token { get; set; }

        public int Interval { get; set; }

        public string PhoneNumber { get; set; }

        public string StatusMessage { get; set; }
    }
}