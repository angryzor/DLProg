using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuaInterface;
using System.Net;

namespace DLProg
{
    public enum LinkAction
    {
        Download,
        Parse
    }

    [Serializable]
    public class Link
    {
        public Uri URL { get; set; }
        public LinkAction Action { get; set; }
        public String DestDir { get; set; }
        public String FileName { get; set; }
        public Object UserData { get; set; }
        public String Error { get; set; }
        public String HTTPMethod { get; set; }
        public String ContentType { get; set; }
        public String Body { get; set; }
    }
}
