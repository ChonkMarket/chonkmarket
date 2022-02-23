using SendGrid;
using SendGrid.Helpers.Mail;
using StockDataLibrary;
using System.Threading.Tasks;

namespace ChonkyWeb.Services
{
    public class EmailSender: IEmailSender
    {
        private ChonkyConfiguration _config;
        public EmailSender(ChonkyConfiguration config)
        {
            _config = config;
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Execute(_config.SendGridKey, subject, message, email);
        }

        public Task Execute(string apiKey, string subject, string message, string email)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("chris@chonk.market", "Chris"),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email));

            msg.SetClickTracking(false, false);

            return client.SendEmailAsync(msg);
        }
    }
}
