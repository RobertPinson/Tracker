using System.Threading.Tasks;

namespace Homeworld.Tracker.Web.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
