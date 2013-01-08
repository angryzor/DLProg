using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace DLProg
{
    public interface ResponseReceiver
    {
        void Receive(Worker w, HttpWebResponse hwr, Link l);
    }
}
