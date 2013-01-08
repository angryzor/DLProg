using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using LuaInterface;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

namespace DLProg
{
    class QueueInterfaceToLua
    {
        Project p;
        LuaHTMLParser h;

        public QueueInterfaceToLua(Project p, LuaHTMLParser h)
        {
            this.p = p;
            this.h = h;
        }

        private Uri relative_to_absolute(String URL)
        {
            Uri u = new Uri(URL,UriKind.RelativeOrAbsolute);
            if (u.IsAbsoluteUri)
                return u;
            else
                return new Uri(h.CurrentLink.URL, u);
        }

        public void add_download_link(String URL, String DestDir, String FileName)
        {
            add_download_link_ex(URL, DestDir, FileName, "GET", null, null);
        }

        public void add_page_link(String URL, Object UserData)
        {
            add_page_link_ex(URL, UserData, "GET", null, null);
        }

        public void push_download_link(String URL, String DestDir, String FileName)
        {
            push_download_link_ex(URL, DestDir, FileName, "GET", null, null);
        }

        public void push_page_link(String URL, Object UserData)
        {
            push_page_link_ex(URL, UserData, "GET", null, null);
        }

        public void add_download_link_ex(String URL, String DestDir, String FileName, String HTTPMethod, String ContentType, String Body)
        {
            if (URL == null)
                throw new LuaException("add_download_link_ex: URL is nil");
            if (DestDir == null)
                throw new LuaException("add_download_link_ex: DestDir is nil");
            if (FileName == null)
                throw new LuaException("add_download_link_ex: FileName is nil");
            if (HTTPMethod == null)
                throw new LuaException("add_download_link_ex: HTTPMethod is nil");
            p.Links.Offer(new Link {    URL = relative_to_absolute(URL),
                                        DestDir = DestDir,
                                        FileName = FileName,
                                        Action = LinkAction.Download,
                                        HTTPMethod = HTTPMethod,
                                        ContentType = ContentType,
                                        Body = Body });
        }

        public void add_page_link_ex(String URL, Object UserData, String HTTPMethod, String ContentType, String Body)
        {
            if (URL == null)
                throw new LuaException("add_page_link_ex: URL is nil");
            if (HTTPMethod == null)
                throw new LuaException("add_page_link_ex: HTTPMethod is nil");
            p.Links.Offer(new Link {    URL = relative_to_absolute(URL),
                                        Action = LinkAction.Parse,
                                        UserData = UserData,
                                        HTTPMethod = HTTPMethod,
                                        ContentType = ContentType,
                                        Body = Body });
        }

        public void push_download_link_ex(String URL, String DestDir, String FileName, String HTTPMethod, String ContentType, String Body)
        {
            if (URL == null)
                throw new LuaException("push_download_link_ex: URL is nil");
            if (DestDir == null)
                throw new LuaException("push_download_link_ex: DestDir is nil");
            if (FileName == null)
                throw new LuaException("push_download_link_ex: FileName is nil");
            if (HTTPMethod == null)
                throw new LuaException("push_download_link_ex: HTTPMethod is nil");
            p.Links.Push(new Link {     URL = relative_to_absolute(URL),
                                        DestDir = DestDir,
                                        FileName = FileName,
                                        Action = LinkAction.Download,
                                        HTTPMethod = HTTPMethod,
                                        ContentType = ContentType,
                                        Body = Body });
        }

        public void push_page_link_ex(String URL, Object UserData, String HTTPMethod, String ContentType, String Body)
        {
            if (URL == null)
                throw new LuaException("push_page_link_ex: URL is nil");
            if (HTTPMethod == null)
                throw new LuaException("push_page_link_ex: HTTPMethod is nil");
            p.Links.Push(new Link {     URL = relative_to_absolute(URL),
                                        Action = LinkAction.Parse,
                                        UserData = UserData,
                                        HTTPMethod = HTTPMethod,
                                        ContentType = ContentType,
                                        Body = Body });
        }
    }

    class PageInterfaceToLua
    {
        public String URL { get; set; }
        public String HTML { get; set; }
        public Object UserData { get; set; }
    }

    class ProjectInterfaceToLua
    {
        private Project p;

        public String DownloadFolder { get; set; }

        public ProjectInterfaceToLua(Project p)
        {
            DownloadFolder = p.RootDirectory;
        }
    }

    class UtilInterfaceToLua
    {
        public String url_escape(String txt)
        {
            return Uri.EscapeDataString(txt);
        }
        public String url_unescape(String txt)
        {
            return Uri.UnescapeDataString(txt);
        }
        public String html_escape(String txt)
        {
            return HttpUtility.HtmlEncode(txt);
        }
        public String html_unescape(String txt)
        {
            return HttpUtility.HtmlDecode(txt);
        }
        public String js_escape(String txt)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in txt)
            {
                switch (c)
                {
                    case '\"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        int i = (int)c;
                        if (i < 32 || i > 127)
                            sb.AppendFormat("\\u{0:X04}", i);
                        else
                            sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }
        public String js_unescape(String txt)
        {
            txt = txt.Replace("\\\\", "\\");
            txt = txt.Replace("\\\"", "\"");
            txt = txt.Replace("\\b", "\b");
            txt = txt.Replace("\\f", "\f");
            txt = txt.Replace("\\n", "\n");
            txt = txt.Replace("\\r", "\r");
            txt = txt.Replace("\\t", "\t");
            Regex rgx = new Regex("\\\\u(\\d\\d\\d\\d)");
            return rgx.Replace(txt,new MatchEvaluator((Match m) => {
                int i = int.Parse(m.Captures[0].Value);
                return ((char)i).ToString();
            }));
        }
    }

    public class LuaHTMLParser : HTMLParser
    {
        Lua lua = new Lua();
        Project p;
        GUIIface gui;
        Link curLink;
        DateTime lastModified_;

        public Link CurrentLink { get { return curLink; } }
        
        public LuaHTMLParser(GUIIface gui, Project p)
        {
            this.p = p;
            this.gui = gui;
/*            string pkgpath = (string)lua["package.path"];
            if (pkgpath == null) pkgpath = "";
            pkgpath += ";" + System.Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar + "lualibs" + System.IO.Path.DirectorySeparatorChar + "?.lua";
            lua["package.path"] = pkgpath;
*/
            lua["project"] = new ProjectInterfaceToLua(p);
            lua["queue"] = new QueueInterfaceToLua(p, this);
            lua["util"] = new UtilInterfaceToLua();
            lua.RegisterFunction("log", this, this.GetType().GetMethod("log"));
            lua.RegisterFunction("io.canwrite", this, this.GetType().GetMethod("CanWrite"));
            lua.RegisterFunction("io.ensurefolder", this, this.GetType().GetMethod("EnsureFolder"));
            lua.RegisterFunction("io.movefile", this, this.GetType().GetMethod("MoveFile"));
            lua.DoString(@"
io.popen = function(prog, mode)
    error('Opening processes is disabled.')
end
do
local realioopen = io.open
local realioclose = io.close
local ensurefolder = io.ensurefolder
local movefile = io.movefile
io.ensurefolder = nil
local openfilenames = {}
io.openfordl = function(filen, mode)
    filen = filen:gsub('/','\\')
    if filen:match('^\\') or filen:match('%.%.\\') or filen:match('^%w:') then
        error('Only relative paths that do not go up (..) are allowed')
    end

    filen = project.DownloadFolder .. '\\' .. filen

    ensurefolder(filen)

    if not io.canwrite(filen) then
        return nil, 'Cannot open file due to user settings.'
    end

    local tmpfilen = filen .. '.dlprog_part'

    local f,err = realioopen(tmpfilen,mode)
    if f then
        openfilenames[f] = filen
    end
    
    return f,err
end
io.open = function(filen, mode)
    filen = filen:gsub('/','\\')
    if filen:match('^\\') or filen:match('%.%.\\') or filen:match('^%w:') then
        error('Only relative paths that do not go up (..) are allowed')
    end

    filen = project.DownloadFolder .. '\\' .. filen

    ensurefolder(filen)

    return realioopen(filen,mode)
end
io.close = function(file)
    local ret = realioclose(file)
    local filen = openfilenames[file]

    if filen then
        movefile(filen .. '.dlprog_part',filen)
        openfilenames[file] = nil
    end

    return ret
end
end
");
        }

        protected override void Parse(String html, Link l, DateTime lastModified)
        {
            try
            {
                curLink = l;
                lua["page"] = new PageInterfaceToLua { HTML = html, URL = l.URL.OriginalString, UserData = l.UserData };
                lastModified_ = lastModified;
                lua.DoString(p.Script);
            }
            catch (LuaException e)
            {
                throw new ApplicationException("Lua error: " + e.Message);
            }
        }


        // Lua exposed global functions
        public void log(String txt)
        {
            if (txt != null)
                gui.PrintLog(txt);
            else
                gui.PrintLog("null");
        }

        public bool CanWrite(string file)
        {
            return p.CanWrite(file, lastModified_);
        }

        public void EnsureFolder(string filen)
        {
            string dir = Path.GetDirectoryName(filen);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public void MoveFile(string filen,string newfilen)
        {
            File.Move(filen, newfilen);
        }
    }
}
