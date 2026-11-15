using HotelWebsite.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Web.Mvc;

namespace HotelWebsite.Controllers
{
    public class FeedbackController : Controller
    {
        [HttpPost]
        public JsonResult SendFeedbackEmail()
        {
            try
            {
                // Read and parse request body as JSON
                string jsonData;
                using (var reader = new System.IO.StreamReader(Request.InputStream))
                {
                    jsonData = reader.ReadToEnd();
                }

                if (string.IsNullOrEmpty(jsonData))
                {
                    return Json(new { success = false, error = "No data received." });
                }

                var feedbackData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);

                if (feedbackData == null || feedbackData.Count == 0)
                {
                    return Json(new { success = false, error = "Invalid feedback format." });
                }

                // Constructing the email content
                var questions = new Dictionary<string, string>
                {
                    {"q1", "Was your stay comfortable and pleasant?"},
                    {"q2", "Did the dining options meet your expectations?"},
                    {"q3", "Was the check-in/check-out process smooth?"},
                    {"q4", "Were the facilities suitable for your needs?"},
                    {"q5", "Would you visit Maisonette Firhill Hotel again?"}
                };

                StringBuilder emailBody = new StringBuilder();
                emailBody.Append("<div style='font-family: Arial, sans-serif;'>");
                emailBody.Append("<img src='https://maisonettefirhillmurree.com/Content/paradise/img/maisonRed.png' alt='Maisonette Logo' height='80' />");
                emailBody.Append("<h3>Feedback from Website</h3>");
                emailBody.Append("<table border='1' style='border-collapse: collapse; width: 100%; text-align: left;'>");
                emailBody.Append("<thead><tr><th>Question</th><th>Response</th></tr></thead><tbody>");

                foreach (var question in questions)
                {
                    string response = feedbackData.ContainsKey(question.Key) ? feedbackData[question.Key] : "No Response";
                    emailBody.Append($"<tr><td>{question.Value}</td><td>{response}</td></tr>");
                }

                emailBody.Append("</tbody></table></div>");

                // Preparing the email
                var message = new MailMessage
                {
                    From = new MailAddress("info@maisonettefirhillmurree.com"),
                    Subject = "Website Feedback",
                    Body = emailBody.ToString(),
                    IsBodyHtml = true
                };
                message.To.Add("info@maisonettefirhillmurree.com");

                var smtp = new SmtpClient("mail.maisonettefirhillmurree.com", 587)
                {
                    Credentials = new System.Net.NetworkCredential("info@maisonettefirhillmurree.com", "Wc1n5!u98"),
                    EnableSsl = false
                };

                smtp.Send(message);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
