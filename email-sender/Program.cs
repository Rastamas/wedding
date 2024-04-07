using System;

using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using MimeKit.Utils;

namespace TestClient
{
    class Program
    {
        private static string FromEmail = string.Empty;

        // Set to false to actually send emails
        private const bool dryRun = true;

        /// <summary>
        /// Dependencies:
        /// - .secrets: plain text file with email password in the first line, from-email in the second, google forms link in the third, website in the fourth
        /// - eskuvo_vendegek.tsv: tab separated file with to-email in the 5th column and name in the 6th
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task Main(string[] args)
        {
            var secrets = await File.ReadAllLinesAsync(".secrets");
            var password = secrets[0];
            FromEmail = secrets[1];
            var facebokEventLink = secrets[2];
            var website = secrets[3];

            var emailTemplate = await File.ReadAllTextAsync("faq_email_template.html");

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync("smtp.gmail.com", 587, false);
            await smtpClient.AuthenticateAsync(FromEmail, password);

            var csvLines = await File.ReadAllLinesAsync("eskuvo_vendegek3.tsv");

            var number = 0;

            System.Console.WriteLine($"Password: {password}, from email: {FromEmail}, forms link: {facebokEventLink}");

            foreach (var line in csvLines.Skip(1))
            {
                var parts = line.Split('\t');
                var toEmail = parts[4].Trim(' ', '"');
                var toName = parts[5].Trim(' ', '"');

                if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(toName))
                {
                    continue;
                }

                Console.WriteLine($"Sending email #{++number} to {toName} to email {toEmail}");
                if (!dryRun)
                {
                    await SendEmail(smtpClient, toEmail, toName, emailTemplate, facebokEventLink, website);
                }
            }

            await smtpClient.DisconnectAsync(true);
        }

        private static async Task SendEmail(SmtpClient smtpClient, string toEmail, string toName, string template, string facebokEventLink, string website)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress("Zsófi és Tomi", FromEmail));
            email.To.Add(new MailboxAddress(toEmail, toEmail));

            email.Subject = "Zsófi és Tomi esküvői információk";

            var builder = new BodyBuilder();

            builder.HtmlBody = string.Format(template,
                toName,
                website,
                facebokEventLink,
                toName.Contains(" és ") ? "kérdésetek" : "kérdésed",
                toName.Contains(" és ") ? "írhattok" : "írhatsz"
                );

            email.Body = builder.ToMessageBody();

            await smtpClient.SendAsync(email);
        }
    }
}
