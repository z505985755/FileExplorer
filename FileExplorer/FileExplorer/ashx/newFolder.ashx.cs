using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Configuration;

namespace FileExplorer
{
    /// <summary>
    /// newFolder 的摘要说明
    /// </summary>
    public class newFolder : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            //postJSON p = new postJSON();
            string fileRootPath = ConfigurationManager.AppSettings["filePath"];
            string path = fileRootPath + "\\" + context.Request["path"];//用户根路径
            string fplderName = fileBLL.createFolder(path,1).Name;
            //p.isError = false;
            //p.obj = fplderName;
            context.Response.Write(fplderName);
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