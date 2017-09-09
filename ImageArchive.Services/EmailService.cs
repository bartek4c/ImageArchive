using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ImageArchive.Services
{
    public class EmailService : IEmailService
    {
        //Email
        private static string _emailFrom;
        private static string _emailTo;
        private static string _smtpServer;
        private static int _smtpPort;
        private static string _smtpLogin;
        private static string _smtpPassword;

        public EmailService(string emailFrom, string emailTo, string smtpServer, int smtpPort, string smtpLogin, string smtpPassword)
        {
            _emailFrom = emailFrom;
            _emailTo = emailTo;
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _smtpLogin = smtpLogin;
            _smtpPassword = smtpPassword;
        }

        public void SendErrorEmail(string message)
        {
            string subject = "Error in ImageArchive processing";

            var sb = new StringBuilder();
            sb.Append("<p>An error has been thrown while processing files in ImageArchive application</p>");
            sb.Append("<hr />");
            sb.Append("<p>" + DateTime.Now.ToString("dd MMM yyyy hh:mm:ss") + "</p>");
            string body = sb.ToString();

            SendEmail(subject, body);
        }

        public void SendErrorEmail(string message, Exception e)
        {
            string subject = "Exception thrown in ImageArchive processing";

            var sb = new StringBuilder();
            sb.Append("<p>An exception has been thrown while processing files in ImageArchive application</p>");
            sb.Append("<hr />");
            sb.Append("<p><strong>Exception details:</strong></p>");
            sb.Append("<p>" + e.Message + "</p>");
            if (e.InnerException != null)
            {
                sb.Append("<p><strong>Inner exception details:</strong></p>");
                sb.Append("<p>" + e.InnerException.Message + "</p>");
            }
            sb.Append("<hr />");
            sb.Append("<p>" + DateTime.Now.ToString("dd MMM yyyy hh:mm:ss") + "</p>");
            string body = sb.ToString();

            SendEmail(subject, body);
        }

        private void SendEmail(string subject, string body)
        {
            var mail = new MailMessage();
			mail.From = new MailAddress(_emailFrom);
			mail.To.Add(_emailTo);
			mail.Subject = subject;
			mail.IsBodyHtml = true;
            mail.Body = body;
            
			var SmtpServer = new SmtpClient(_smtpServer);
            SmtpServer.Port = _smtpPort;
			SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
			SmtpServer.UseDefaultCredentials = false;
			SmtpServer.Credentials = new System.Net.NetworkCredential(_smtpLogin, _smtpPassword);
			SmtpServer.EnableSsl = true;
			SmtpServer.Send(mail);
        }
    }
}
