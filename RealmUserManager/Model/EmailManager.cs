using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Newtonsoft.Json;

namespace RealmUserManager.Model
{
        public class EmailSettings
        {
            public string FromName;
            public string FromEmailAdress;
            public Dictionary<string, string> Subject;
            public Dictionary<string, string> MessagePath;
        }


        public class EmailManager
        {
            private readonly IAppConfiguration _config;
            private readonly EmailSettings emailSettings;

            public EmailManager(string templateFilenName, IAppConfiguration config)
            {
                _config = config;
                // deserialize JSON directly from a file
                using (StreamReader file = File.OpenText(Path.Combine("Content", templateFilenName)))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    emailSettings = (EmailSettings)serializer.Deserialize(file, typeof(EmailSettings));

                }
            }

            public async Task SendActivationEmail(string emailAdress, string userId, string languageID)
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(emailSettings.FromName, emailSettings.FromEmailAdress));
                message.To.Add(new MailboxAddress(emailAdress, emailAdress));

                string subject;
                if (!emailSettings.Subject.TryGetValue(languageID, out subject))
                {
                    subject = emailSettings.Subject["en"];
                }

                message.Subject = subject;


                string HtmlMessagePath;
                if (!emailSettings.MessagePath.TryGetValue(languageID, out HtmlMessagePath))
                {
                    HtmlMessagePath = emailSettings.MessagePath["en"];
                }



                message.Body = new TextPart("html")
                {
                    Text = File.ReadAllText(Path.Combine("Content", HtmlMessagePath)).Replace("userid", userId + "?lang=" + languageID)
                };


                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_config.Smtp.Server, _config.Smtp.Port, SecureSocketOptions.None);

                    // Note: since we don't have an OAuth2 token, disable
                    // the XOAUTH2 authentication mechanism.
                    client.AuthenticationMechanisms.Remove("XOAUTH2");


                    // Note: only needed if the SMTP server requires authentication
                    await client.AuthenticateAsync(_config.Smtp.User, _config.Smtp.Pass);

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);

                }

            }

            public Task SendActivationEmail(UserData userToActivate)
            {
                throw new System.NotImplementedException();
            }
        }
    }