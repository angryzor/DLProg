using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Net;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace DLProg
{
    public class WorkerProgress : INotifyPropertyChanged
    {
        private long bytesDownloaded_;
        private long bytesTotal_;

        public long BytesDownloaded
        {
            get
            {
                return bytesDownloaded_;
            }
            set
            {
                bytesDownloaded_ = value;
                NotifyPropertyChanged("BytesDownloaded");
                NotifyPropertyChanged("BytesTodo");
                NotifyPropertyChanged("PercentageComplete");
            }
        }
        public long BytesTotal
        {
            get
            {
                return bytesTotal_;
            }
            set
            {
                bytesTotal_ = value;
                NotifyPropertyChanged("BytesTotal");
                NotifyPropertyChanged("BytesTodo");
                NotifyPropertyChanged("PercentageComplete");
            }
        }
        public long BytesTodo { get { return BytesTotal - BytesDownloaded; } }
        public float PercentageComplete
        { 
            get 
            {
                if (BytesTotal == 0)
                    return 0;
                else
                    return (float)(BytesDownloaded * 100 / BytesTotal);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Reset()
        {
            BytesDownloaded = 0;
            BytesTotal = 0;
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }

    public class Worker : INotifyPropertyChanged
    {
        private Project p;
        private Uri curUri_;
        private Thread workThread_;
        private bool isRunning_ = false;
        private WorkerProgress progress_ = new WorkerProgress();

        public HTMLParser Parser { get; set; }
        public FileWriter Writer { get; set; }
        public Uri CurrentUri { get { return curUri_; } }
        public WorkerProgress Progress { get { return progress_; } }

        public event PropertyChangedEventHandler PropertyChanged;

        public Worker(GUIIface gui, Project p)
        {
            this.p = p;
            Parser = new LuaHTMLParser(gui, p);
            Writer = new FileWriter(p);

            Progress.PropertyChanged += new PropertyChangedEventHandler(Progress_PropertyChanged);
        }

        void Progress_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("Progress");
        }

        public void Start()
        {
            isRunning_ = true;
            (workThread_ = new Thread(new ThreadStart(WorkerLoop))).Start();
        }

        public void Stop()
        {
            isRunning_ = false;
        }

        private void WorkerLoop()
        {
            while (isRunning_)
            {
                Link l = p.Links.Pop();
                if (l == null)
                {
                    Thread.Sleep(10);
                    continue;
                }

                DownloadLink(l);

                Thread.Sleep(1000 * (int)Properties.Settings.Default.DownloadWaitTime);
            }
        }

        private void FillHTTPWebRequest(HttpWebRequest hwr, Link l, CookieContainer cookies)
        {
            hwr.CookieContainer = cookies;
            hwr.Method = l.HTTPMethod;
            hwr.ContentType = l.ContentType;
            hwr.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:6.0) Gecko/20100101 Firefox/6.0";
            hwr.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            hwr.Headers.Add("Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.7");
            hwr.Headers.Add("Accept-Language: nl, en-gb;q=0.9, en;q=0.8");

            if (l.Body != null && l.Body != "")
            {
                Stream s = hwr.GetRequestStream();
                using (StreamWriter sw = new StreamWriter(s))
                {
                    sw.Write(l.Body);
                }
            }
        }

        private CookieContainer MakeLocalCookies(Uri url)
        {
            CookieContainer localCookies = new CookieContainer();
            Workers.RetrieveIECookiesForUri(url, localCookies);

            lock (Workers.cookies)
            {
                localCookies.Add(Workers.cookies.GetCookies(url));
            }

            return localCookies;
        }

        private void DownloadLink(Link l)
        {
            try
            {
                curUri_ = l.URL;
                NotifyPropertyChanged("CurrentUri");

                CookieContainer localCookies = MakeLocalCookies(l.URL);

                HttpWebRequest hwr = (HttpWebRequest)HttpWebRequest.Create(l.URL);
                FillHTTPWebRequest(hwr, l, localCookies);
                using (HttpWebResponse hwrs = (HttpWebResponse)hwr.GetResponse())
                {
                    lock (Workers.cookies)
                    {
                        Workers.cookies.Add(hwrs.Cookies);
                    }

                    switch (l.Action)
                    {
                        case LinkAction.Download:
                            Writer.Receive(this,hwrs, l);
                            break;
                        case LinkAction.Parse:
                            Parser.Receive(this,hwrs, l);
                            break;
                    }
                }
            }
            catch (UriFormatException e)
            {
                l.Error = e.Message;
                p.BadLinks.Offer(l);
            }
            catch (WebException e)
            {
                l.Error = e.Message;
                p.BadLinks.Offer(l);
            }
            catch (Exception e)
            {
                l.Error = e.Message;
                p.BadLinks.Offer(l);
            }

            curUri_ = null;
            NotifyPropertyChanged("CurrentUri");
            Progress.Reset();
        }


        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }

    public class Workers
    {
        private ObservableCollection<Worker> workers = new ObservableCollection<Worker>();
        private GUIIface gui;
        private Project p;
        private bool isRunning = false;

        public bool IsRunning { get { return isRunning; } }
        public ObservableCollection<Worker> WorkerCollection { get { return workers; } }

        public static CookieContainer cookies = new CookieContainer();

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref int pcchCookieData, int dwFlags, IntPtr lpReserved);

        public Workers(Project p, GUIIface gui)
        {
            this.p = p;
            this.gui = gui;
        }


        public void Start()
        {
            if (isRunning)
                return;
            
            GetLoginInfo();
            if (p.Links.Count == 0)
            {
                if (p.StartingUri == null)
                    throw new InvalidOperationException("No starting URI specified!");

                p.Links.Add(new Link { URL = p.StartingUri, Action = LinkAction.Parse, HTTPMethod = "GET" });
            }
            StartWorkers();
            isRunning = true;
        }

        public void Stop()
        {
            if (!isRunning)
                return;
            StopWorkers();
            isRunning = false;
        }

        private void StartWorkers()
        {
            uint num_workers = Properties.Settings.Default.DownloadNumWorkers;
            if (workers.Count > num_workers)
                for (uint i = num_workers; i < workers.Count; i++)
                    workers.RemoveAt((int)i);
            else if(workers.Count < num_workers)
                for (int i = workers.Count; i < num_workers; i++)
                    workers.Add(new Worker(gui, p));

            foreach (Worker t in workers)
                t.Start();
        }

        private void StopWorkers()
        {
            foreach (Worker t in workers)
                t.Stop();
        }

        private void GetLoginInfo()
        {
            Uri lUrl = p.LoginUri;
            if (lUrl != null)
            {
                LoginPopup lp = new LoginPopup(lUrl);
                lp.ShowDialog();
                RetrieveIECookiesForUri(lUrl, cookies);
            }
        }

        public static void RetrieveIECookiesForUri(Uri uri, CookieContainer target)
        {
            StringBuilder cookieHeader = new StringBuilder(new String(' ', 256), 256);
            int datasize = cookieHeader.Length;
            if (!InternetGetCookieEx(uri.AbsoluteUri, null, cookieHeader, ref datasize, 0x2000, IntPtr.Zero))
            {
                if (datasize < 0)
                    return;
                cookieHeader = new StringBuilder(datasize);
                InternetGetCookieEx(uri.AbsoluteUri, null, cookieHeader, ref datasize, 0x2000, IntPtr.Zero);
            }
            target.SetCookies(uri, cookieHeader.ToString().Replace(";", ","));
        }

    }
}
