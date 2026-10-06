using System.Net;
using System.Net.Mail;

namespace TemsGroupProject.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode)
        {
            try
            {
                // TEMPORARY: while real email isn't working, write the OTP to a log file
                // instead of sending it. Set Smtp:UseFileLogging to false in appsettings.json
                // once Brevo (or another provider) is working.
                var useFileLogging = bool.Parse(_config["Smtp:UseFileLogging"] ?? "false");
                if (useFileLogging)
                {
                    _logger.LogInformation("OTP for {Email}: {Otp}", toEmail, otpCode);
                    return true;
                }

                var smtpHost = _config["Smtp:Host"];
                var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
                var smtpUser = _config["Smtp:Username"];
                var smtpPass = _config["Smtp:Password"];
                var fromEmail = _config["Smtp:FromEmail"] ?? smtpUser;
                var enableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");

                if (string.IsNullOrWhiteSpace(smtpHost) ||
                    string.IsNullOrWhiteSpace(smtpUser) ||
                    string.IsNullOrWhiteSpace(smtpPass))
                {
                    _logger.LogError("SMTP settings are missing or incomplete in appsettings.json.");
                    return false;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = enableSsl,
                    Timeout = 15000 // 15 seconds, instead of the 100s default
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail!, "TemsGroupProject"),
                    Subject = "Your login verification code",
                    Body = $"Your one-time verification code is: {otpCode}\n\nThis code expires in 5 minutes. If you did not request this, you can ignore this email.",
                    IsBodyHtml = false
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                return true;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error while sending OTP email to {Email}", toEmail);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending OTP email to {Email}", toEmail);
                return false;
            }
        }
    }
}