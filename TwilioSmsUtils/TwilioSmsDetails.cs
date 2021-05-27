using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TwilioSmsUtils
{
    public class TwilioSmsDetails
    {
        public string Sid { get; private set; }
        public DateTimeOffset DateCreated { get; private set; }
        public DateTimeOffset DateUpdated { get; private set; }
        public string Body { get; private set; }
        public string To { get; private set; }
        public string From { get; private set; }
        public string Uri { get; private set; }
        internal static TwilioSmsDetails FromXElement(XElement xElement)
        {
            return (new TwilioSmsDetails()
            {
                Sid = (string)xElement.Element("Sid"),
                DateCreated = DateTimeOffset.Parse(xElement.Element("DateCreated").Value),
                DateUpdated = DateTimeOffset.Parse(xElement.Element("DateUpdated").Value),
                Body = (string)xElement.Element("Body"),
                To = (string)xElement.Element("To"),
                From = (string)xElement.Element("From"),
                Uri = (string)xElement.Element("Uri")
            });
        }
    }
}
