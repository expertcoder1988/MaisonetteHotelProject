using System.Text;
using System.Web.Mvc;
//using System.ServiceModel.Syndication;
using System.Xml;
using System.Web;
using System.ServiceModel.Syndication;

namespace MyBlog.Model
{
    public class XmlResult : ActionResult
    {
        private object objectToSerialize;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlResult"/> class.
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize to XML.</param>
        public XmlResult(object objectToSerialize)
        {
            this.objectToSerialize = objectToSerialize;
        }

        /// <summary>
        /// Gets the object to be serialized to XML.
        /// </summary>
        public object ObjectToSerialize
        {
            get { return this.objectToSerialize; }
        }

        /// <summary>
        /// Serialises the object that was passed into the constructor to XML and writes the corresponding XML to the result stream.
        /// </summary>
        /// <param name="context">The controller context for the current request.</param>
        public override void ExecuteResult(ControllerContext context)
        {
            if (this.objectToSerialize != null)
            {
                context.HttpContext.Response.Clear();
                var xs = new System.Xml.Serialization.XmlSerializer(this.objectToSerialize.GetType());
                context.HttpContext.Response.ContentType = "text/xml";
                xs.Serialize(context.HttpContext.Response.Output, this.objectToSerialize);
            }
        }
    }

    /// <summary>
    /// Represents a class that is used to render an Atom 1.0 feed by using an <see cref="SyndicationFeed"/> instance 
    /// representing the feed.
    /// </summary>
    public class AtomResult : ActionResult
    {
        private readonly Atom10FeedFormatter syndicationFeed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AtomActionResult"/> class.
        /// </summary>
        /// <param name="syndicationFeed">The Atom 1.0 <see cref="SyndicationFeed" />.</param>
        public AtomResult(Atom10FeedFormatter syndicationFeed)
        {
            this.syndicationFeed = syndicationFeed;
        }

        /// <summary>
        /// Executes the call to the ActionResult method and returns the created feed to the output response.
        /// </summary>
        /// <param name="context">The context in which the result is executed. The context information includes the 
        /// controller, HTTP content, request context, and route data.</param>
        public override void ExecuteResult(ControllerContext context)
        {
            context.HttpContext.Response.ContentType = "application/atom+xml";
            Atom10FeedFormatter feedFormatter = this.syndicationFeed;
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Encoding = Encoding.UTF8;

            if (HttpContext.Current.IsDebuggingEnabled)
            {
                // Indent the XML for easier viewing but only in Debug mode. In Release mode, everything is output on 
                // one line for best performance.
                xmlWriterSettings.Indent = true;
            }

            using (XmlWriter xmlWriter = XmlWriter.Create(context.HttpContext.Response.Output, xmlWriterSettings))
            {
                feedFormatter.WriteTo(xmlWriter);
            }
        }
    }
}
