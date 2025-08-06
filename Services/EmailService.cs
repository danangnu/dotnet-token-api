using System.Net;
using System.Net.Mail;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var fromEmail = _config["EmailSettings:From"];
        var smtpHost = _config["EmailSettings:SmtpHost"];
        var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"]);
        var smtpUser = _config["EmailSettings:SmtpUser"];
        var smtpPass = _config["EmailSettings:SmtpPass"];

        using var message = new MailMessage(fromEmail, toEmail)
        {
            Subject = "Reset your password",
            Body = $"""
                Hello,

                We received a request to reset your password.
                Please click the link below to reset it:

                {resetLink}

                If you didn’t request this, you can ignore this email.

                Thanks,
                Your App Team
            """,
            IsBodyHtml = false
        };

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        await client.SendMailAsync(message);
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verificationLink)
    {
        var smtpHost = _config["Smtp:Host"];
        var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
        var smtpUser = _config["Smtp:Username"];
        var smtpPass = _config["Smtp:Password"];
        var fromEmail = _config["Smtp:FromEmail"] ?? smtpUser;

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        var message = new MailMessage(fromEmail, toEmail)
        {
            Subject = "Verify your email address",
            Body = $"Please verify your email by clicking this link: {verificationLink}",
            IsBodyHtml = false
        };

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email.");
            throw;
        }
    }
}
