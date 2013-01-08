using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace DLProg
{
    public enum OverwriteMode
    {
        Never,
        IfNewer,
        Always
    }

    public class FileWriter : ResponseReceiver
    {
        Project p;

        public FileWriter(Project p)
        {
            this.p = p;
        }

        public void Receive(Worker w, HttpWebResponse hwr, Link l)
        {
            char[] seps = {'/'};
            
            Regex badpath = new Regex(String.Format("[{0}]", Regex.Escape(new String(Path.GetInvalidPathChars()))));
            Regex badfn = new Regex(String.Format("[{0}]", Regex.Escape(new String(Path.GetInvalidFileNameChars()))));

            String destdir = badpath.Replace(l.DestDir, "_");
            String filename = badfn.Replace(l.FileName, "_");

            String dir = Path.Combine(p.RootDirectory,Path.Combine(destdir.Split(seps)));
            String file = Path.Combine(dir, filename);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            
            if (!p.CanWrite(file, hwr.LastModified))
                    return;

            String tmpfile = file + ".dlprog_part";

            using (FileStream fs = File.Open(tmpfile, FileMode.Create))
            {
                Stream s = hwr.GetResponseStream();
                //s.CopyTo(fs);
                const int BUFFERSIZE = 8192;
                w.Progress.BytesTotal = hwr.ContentLength;
                byte[] buf = new byte[BUFFERSIZE];
                int bytesread = 0;

                while ((bytesread = s.Read(buf, 0, BUFFERSIZE)) != 0)
                {
                    fs.Write(buf, 0, bytesread);
                    
                    w.Progress.BytesDownloaded += bytesread;
                }
            }

            File.Move(tmpfile, file);
        }
    }
}
