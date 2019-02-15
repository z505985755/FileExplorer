using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Configuration;

namespace FileExplorer
{
    /// <summary>
    /// stickFiles 的摘要说明
    /// </summary>
    public class stickFiles : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            string fileRootPath = ConfigurationManager.AppSettings["filePath"];
            string oldPath = context.Request["oldPath"];
            string newPath = context.Request["newPath"];
            string names = context.Request["names"];
            string[] nameArr = names.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            bool isCopy = context.Request["isCopy"] == "true";
            oldPath = fileRootPath + "\\" + oldPath + "\\";
            oldPath = oldPath.Replace("/", "\\").Replace(@"\\", "\\");
            newPath = fileRootPath + "\\" + newPath + "\\";
            newPath = newPath.Replace("/", "\\").Replace(@"\\", "\\");
            if (isCopy)
            {
                long filesLength = 0;
                foreach (var item in nameArr)
                {
                    if (File.Exists(oldPath + item))
                    {
                        FileInfo fi = new FileInfo(oldPath + item);
                        filesLength += fi.Length;
                    }
                    else
                    {
                        filesLength += fileBLL.getFolderSize(oldPath + item);
                    }
                }
                foreach (var item in nameArr)
                {
                    string nitem = item;
                    if (oldPath + item == newPath + item && Directory.Exists(oldPath + item))
                    {
                        nitem = fileBLL.getNotRepeatFolderName(newPath, item, 1);
                    }
                    fileBLL.copyFiles(oldPath + item, newPath + nitem);
                }
            }
            else
            {
                foreach (var item in nameArr)
                {
                    fileBLL.move(oldPath + item, newPath + item);
                }
            }
            context.Response.Write("{\"error\":false}");
            context.Response.End();
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}