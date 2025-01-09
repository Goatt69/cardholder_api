using System.Net;
using System.Net.Mail;

namespace cardholder_api.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly SmtpClient _smtpClient;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpClient = new SmtpClient
        {
            Host = configuration["EmailSettings:SmtpHost"],
            Port = int.Parse(configuration["EmailSettings:SmtpPort"]),
            EnableSsl = true,
            Credentials = new NetworkCredential(
                configuration["EmailSettings:SmtpUser"],
                configuration["EmailSettings:SmtpPass"]
            )
        };
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_configuration["EmailSettings:FromEmail"]),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(to);

        await _smtpClient.SendMailAsync(mailMessage);
    }
}