using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace DLProg
{
    public abstract class HTMLParser : ResponseReceiver
    {
        public void Receive(Worker w, HttpWebResponse hwr, Link l)
        {
            StreamReader sr = new StreamReader(hwr.GetResponseStream());
            Parse(sr.ReadToEnd(), l, hwr.LastModified);
            sr.Close();
        }
        protected abstract void Parse(String html, Link l, DateTime lastModified);
    }
}
