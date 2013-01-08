using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace DLProg
{
    public interface GUIIface
    {
        void PrintLog(String txt);
        Dispatcher GetDispatcher();
    }
}
