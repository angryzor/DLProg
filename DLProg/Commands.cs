using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace DLProg
{
    class Commands
    {
        static RoutedCommand start = new RoutedCommand();
        static RoutedCommand stop = new RoutedCommand();
        static RoutedCommand clearqueue = new RoutedCommand();
        static RoutedCommand requeueerrors = new RoutedCommand();
        static RoutedCommand clearerrors = new RoutedCommand();

        public static RoutedCommand Start { get { return start; } }
        public static RoutedCommand Stop { get { return stop; } }
        public static RoutedCommand ClearQueue { get { return clearqueue; } }
        public static RoutedCommand RequeueErrors { get { return requeueerrors; } }
        public static RoutedCommand ClearErrors { get { return clearerrors; } }
    }
}
