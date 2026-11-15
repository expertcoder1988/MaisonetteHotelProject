using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyBlog.Model
{
    public static class Log
    {
        public static void Write(HttpApplication application)
        {
            if (application != null)
            {
                try
                {
                    Exception exception = application.Server.GetLastError();
                    HttpRequest request = application.Request;
                    WriteLog(exception, request, "");
                }
                catch
                {
                }
            }
        }

        public static void Write(Exception exception, string message = "")
        {
            try
            {
                HttpRequest request = HttpContext.Current.Request;
                WriteLog(exception, request, message);
            }
            catch
            {
            }
        }

        public static void Write(string message)
        {
            try
            {
                HttpRequest request = HttpContext.Current.Request;
                WriteLog(null, request, message);
            }
            catch
            {
            }
        }

        private static void WriteLog(Exception exception, HttpRequest request, string message)
        {
            try
            {
                string errorLogFolderPath = AppDomain.CurrentDomain.BaseDirectory + "ErrorLogs\\" + DateTime.Now.ToString("MMM_yyyy") + "\\";
                IO.CreateDirectoryIfMissing(errorLogFolderPath);
                string errorFilePath = errorLogFolderPath + string.Format("log_{0}.html", DateTime.Now.ToString("MMM_dd_yyyy"));

                using (System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(new System.IO.FileStream(errorFilePath, System.IO.FileMode.Append, System.IO.FileAccess.Write)))
                {
                    StringBuilder strMessage = new StringBuilder();
                    strMessage.Append("<h2>Error: </h2>" + DateTime.Now);
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        strMessage.Append("<p>Message: </p>" + message);
                    }
                    strMessage.Append(GerError(exception, request));
                    strMessage.Append("<hr/>");
                    streamWriter.WriteLine(string.Empty);
                    streamWriter.WriteLine(strMessage);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
            catch
            {
            }
        }

        private static string GerError(Exception exception, HttpRequest request)
        {
            StringBuilder errorHTML = new StringBuilder();
            errorHTML.Append("<ul>");
            errorHTML.AppendFormat("<li>URL:     {0}</li>", request.Url.ToString());
            errorHTML.AppendFormat("<li>RAW URL: {0}</li>", request.RawUrl.ToString());
            errorHTML.AppendFormat("<li>TYPE:    {0}</li>", request.HttpMethod);
            errorHTML.AppendFormat("<li>REF:     {0}</li>", request.ServerVariables["HTTP_REFERER"]);
            errorHTML.AppendFormat("<li>COOKIE:<br/>{0}</li>", request.ServerVariables["HTTP_COOKIE"]);
            errorHTML.AppendFormat("<li>USER AGENT:<br/>{0}</li>", request.ServerVariables["HTTP_USER_AGENT"]);
            errorHTML.AppendFormat("<li>Site PATH:<br/>{0}</li>", request.ServerVariables["APPL_PHYSICAL_PATH"]);
            errorHTML.AppendFormat("<li>Error Details:<br/>{0}</li>", exception.GetBaseException());
            errorHTML.Append("</ul>");

            return errorHTML.ToString();
        }

        public static string ToLogString(this Exception ex, string additionalMessage)
        {
            StringBuilder msg = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(additionalMessage))
            {
                msg.Append(additionalMessage);
                msg.Append(Environment.NewLine);
            }

            if (ex != null)
            {
                try
                {
                    Exception orgEx = ex;

                    msg.Append("Exception:");
                    msg.Append(Environment.NewLine);
                    while (orgEx != null)
                    {
                        msg.Append(orgEx.Message);
                        msg.Append(Environment.NewLine);
                        orgEx = orgEx.InnerException;
                    }

                    if (ex.Data != null)
                    {
                        foreach (object i in ex.Data)
                        {
                            msg.Append("Data :");
                            msg.Append(i.ToString());
                            msg.Append(Environment.NewLine);
                        }
                    }

                    if (ex.StackTrace != null)
                    {
                        msg.Append("StackTrace:");
                        msg.Append(Environment.NewLine);
                        msg.Append(ex.StackTrace.ToString());
                        msg.Append(Environment.NewLine);
                    }

                    if (ex.Source != null)
                    {
                        msg.Append("Source:");
                        msg.Append(Environment.NewLine);
                        msg.Append(ex.Source);
                        msg.Append(Environment.NewLine);
                    }

                    if (ex.TargetSite != null)
                    {
                        msg.Append("TargetSite:");
                        msg.Append(Environment.NewLine);
                        msg.Append(ex.TargetSite.ToString());
                        msg.Append(Environment.NewLine);
                    }

                    Exception baseException = ex.GetBaseException();
                    if (baseException != null)
                    {
                        msg.Append("BaseException:");
                        msg.Append(Environment.NewLine);
                        msg.Append(ex.GetBaseException());
                    }
                }
                finally
                {
                }
            }
            return msg.ToString();
        }

    }
}
