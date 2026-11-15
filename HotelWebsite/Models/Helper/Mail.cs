using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyBlog.Model
{
    /// <summary>
    /// Class Mail.
    /// </summary>
    public static class Mail
    {
        /// <summary>
        /// Sends the specified subject.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="body">The body.</param>
        /// <param name="from">From.</param>
        /// <param name="fromTitle">From title.</param>
        /// <param name="recipients">The recipients.</param>
        /// <param name="cc">The cc.</param>
        /// <param name="bcc">The BCC.</param>
        /// <param name="attachments">The attachments.</param>
        public static void Send(string subject, string body, string from, string fromDisplayName, List<string> recipients = null, List<string> cc = null, List<string> bcc = null, List<string> attachments = null)
        {
            Send(subject, body, from, fromDisplayName, string.Join(",", recipients), string.Join(",", cc), string.Join(",", bcc), attachments);
        }

        /// <summary>
        /// Sends the specified subject.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="body">The body.</param>
        /// <param name="from">From.</param>
        /// <param name="fromTitle">From title.</param>
        /// <param name="recipients">The recipients.</param>
        /// <param name="cc">The cc.</param>
        /// <param name="bcc">The BCC.</param>
        /// <param name="attachments">The attachments.</param>
        public static void Send(string subject, string body, string from, string fromDisplayName, string recipients, string cc = "", string bcc = "", List<string> attachments = null)
        {

            try
            {
                if (!string.IsNullOrWhiteSpace(recipients) || !string.IsNullOrWhiteSpace(bcc))
                {
                    MailMessage mailmessage = new MailMessage();
                    SmtpClient oSmtpClient = new SmtpClient();

                    mailmessage.From = new MailAddress(from, fromDisplayName);

                    if (!string.IsNullOrWhiteSpace(recipients))
                        mailmessage.To.Add(recipients);

                    if (!string.IsNullOrWhiteSpace(cc))
                        mailmessage.CC.Add(cc);

                    if (!string.IsNullOrWhiteSpace(bcc))
                        mailmessage.Bcc.Add(bcc);

                    mailmessage.Subject = subject;
                    mailmessage.Body = body;
                    mailmessage.IsBodyHtml = true;

                    if (attachments != null)
                    {
                        foreach (string file in attachments)
                        {
                            if (IO.FileExists(file))
                            {
                                mailmessage.Attachments.Add(new Attachment(file));
                            }
                        }
                    }


                    // If in gmail email not received email then Less secure apps set ON in gmail
                    // https://myaccount.google.com/lesssecureapps
                    // Allow less secure apps: ON
                    // [[mailSettings]]
                    var setting = CommonLogic.GetSetting();

                    if (!string.IsNullOrWhiteSpace(setting.SMTPEmail) && !string.IsNullOrWhiteSpace(setting.SMTPPassword)
                        && !string.IsNullOrWhiteSpace(setting.SMTPHost) && setting.SMTPPort > 0)
                    {
                        oSmtpClient.UseDefaultCredentials = false;
                        oSmtpClient.Credentials = new NetworkCredential(setting.SMTPEmail, setting.SMTPPassword);
                        oSmtpClient.Host = setting.SMTPHost; //"smtp.gmail.com";
                        oSmtpClient.Port = setting.SMTPPort; //587;
                        oSmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        oSmtpClient.EnableSsl = setting.SMTPEnableSsl;
                        //oSmtpClient.Send(mailmessage);
                        oSmtpClient.SendCompleted += (s, e) => { oSmtpClient.Dispose(); mailmessage.Dispose(); };
                        ThreadPool.QueueUserWorkItem(o => oSmtpClient.SendAsync(mailmessage, Tuple.Create(oSmtpClient, mailmessage)));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
