using System.Threading.Tasks;

namespace Homeworld.Tracker.Web.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}
