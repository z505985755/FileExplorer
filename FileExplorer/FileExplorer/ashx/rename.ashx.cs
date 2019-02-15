using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace FileExplorer
{
    /// <summary>
    /// rename 的摘要说明
    /// </summary>
    public class rename : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            string fileRootPath = ConfigurationManager.AppSettings["filePath"];
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string path = context.Request["path"];
            string newName = context.Request["newName"];
            string oldName = context.Request["oldName"];
            string newPath = fileRootPath +  "\\" + path + "\\" + newName;//原路径
            string oldPath = fileRootPath + path + "\\" + oldName;//原路径
            PostJSON p = new PostJSON();
            try
            {
                System.IO.Directory.Move(oldPath, newPath);
            }
            catch (Exception ex)
            {
                p.isError = true;
                p.errorMsg = ex.ToString();
            }

            context.Response.Write(jss.Serialize(p));
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