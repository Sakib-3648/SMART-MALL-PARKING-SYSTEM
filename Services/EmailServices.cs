using System.Net;
using System.Net.Mail;

namespace SMART_MALL_PARKING_SYSTEM.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationCodeAsync(
            string recipientEmail,
            string verificationCode)
        {
            var senderEmail =
                _configuration["EmailSettings:SenderEmail"];

            var appPassword =
                _configuration["EmailSettings:AppPassword"];

            var smtpServer =
                _configuration["EmailSettings:SmtpServer"];

            var port =
                int.Parse(_configuration["EmailSettings:Port"]!);

            using var smtpClient = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(
                    senderEmail,
                    appPassword),

                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(
                    senderEmail!,
                    "Smart Mall Parking"),

                Subject = "Email Verification Code",

                Body = $@"Welcome to Smart Mall Parking!

Your verification code is: {verificationCode}

This code is required to complete your registration.",

                IsBodyHtml = false
            };

            mailMessage.To.Add(recipientEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}