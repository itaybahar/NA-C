using System.Threading.Tasks;

namespace Domain_Project.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
