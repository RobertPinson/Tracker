using System.ComponentModel.DataAnnotations;

namespace Homeworld.Tracker.Web.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
