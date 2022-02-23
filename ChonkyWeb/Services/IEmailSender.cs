using System.Threading.Tasks;

namespace ChonkyWeb.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}