using System.Net;
using System.Net.Mail;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        var message = CreateMailMessage(toEmail, subject, htmlMessage);
        using var client = GetSmtpClient();
        await client.SendMailAsync(message);
    }

    public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string htmlMessage, byte[] attachmentBytes, string attachmentName)
    {
        var message = CreateMailMessage(toEmail, subject, htmlMessage);
        message.Attachments.Add(new Attachment(new MemoryStream(attachmentBytes), attachmentName));
        using var client = GetSmtpClient();
        await client.SendMailAsync(message);
    }

    private MailMessage CreateMailMessage(string toEmail, string subject, string htmlMessage)
    {
        return new MailMessage
        {
            From = new MailAddress(_config["EmailSettings:From"]),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true,
            To = { toEmail }
        };
    }

    private SmtpClient GetSmtpClient()
    {
        return new SmtpClient
        {
            Host = _config["EmailSettings:SmtpHost"],
            Port = int.Parse(_config["EmailSettings:Port"]),
            EnableSsl = true,
            Credentials = new NetworkCredential(
                _config["EmailSettings:Username"],
                _config["EmailSettings:Password"])
        };
    }
}
