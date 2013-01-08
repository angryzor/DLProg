using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace DLProg
{
    [Serializable]
    public class Project
    {
        // Private Members
        private LinkQueue linkQueue_;
        private LinkQueue badLinkQueue_;
        public ObservableCollection<Uri> startingUris_;

        // Kind of static properties
        public String RootDirectory { get; set; }
        public String Script { get; set; }
        public Uri LoginUri { get; set; }
        public OverwriteMode Overwrite { get; set; }

        // Dynamic properties
        public LinkQueue Links { get { return linkQueue_; } }
        public LinkQueue BadLinks { get { return badLinkQueue_; } }
        public ObservableCollection<Uri> StartingUris { get { return startingUris_; } }

        public Project(GUIIface gui, String rootDir)
        {
            linkQueue_ = new LinkQueue(gui.GetDispatcher());
            badLinkQueue_ = new LinkQueue(gui.GetDispatcher());
            startingUris_ = new ObservableCollection<Uri>();
            RootDirectory = rootDir;
        }

        public void RetryBadLinks()
        {
            foreach (Link l in badLinkQueue_)
                linkQueue_.Add(l);
            badLinkQueue_.Clear();
        }

        public static Project Load(GUIIface gui, String proj)
        {
            FileStream fs = new FileStream(proj, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryFormatter formatter = new BinaryFormatter();
            Project p = (Project)formatter.Deserialize(fs);
            p.ResetVolatileData(gui);
            fs.Close();
            return p;
        }

        private void ResetVolatileData(GUIIface gui)
        {
            linkQueue_.SetDispatcher(gui.GetDispatcher());
            badLinkQueue_.SetDispatcher(gui.GetDispatcher());
        }

        public void Save(String proj)
        {
            FileStream fs = new FileStream(proj, FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(fs, this);
            fs.Close();
        }

        public bool CanWrite(String file, DateTime lastModified)
        {
            if (!File.Exists(file))
                return true;

            switch (Overwrite)
            {
                case OverwriteMode.Never:
                    return false;
                case OverwriteMode.Always:
                    return true;
                case OverwriteMode.IfNewer:
                    return File.GetLastWriteTime(file) < lastModified;
            }
            return false;
        }

        public void LoadStartingUrisFromText(String txt)
        {
            Regex rx = new Regex("(http://)?[%a.]+(/[-+_%a%%%d]+)*(\\?[-_%a%d])?(#)?");
            
        }
    }
}
