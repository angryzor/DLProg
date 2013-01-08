using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLProg
{
    class ContentTypeMismatchException : ApplicationException
    {
        ContentTypeMismatchException() : base("This HTTP response receiver cannot process the Content Type supplied to it.")
        {
        }
    }
}
