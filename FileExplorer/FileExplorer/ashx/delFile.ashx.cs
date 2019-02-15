using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.IO;
using System.Configuration;

namespace FileExplorer
{
    /// <summary>
    /// delFile 的摘要说明
    /// </summary>
    public class delFile : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            string fileRootPath = ConfigurationManager.AppSettings["filePath"];
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string path = context.Request["path"];
            path = fileRootPath + "\\" + path;//原路径
            string names = context.Request["names"];
            string[] namearr = names.Split('/');
            PostJSON p = new PostJSON();
            for (int i = 0; i < namearr.Length; i++)
            {
                if (!string.IsNullOrEmpty(namearr[i]))
                {
                    try
                    {
                        string npath = path + "\\" + namearr[i];
                        if (Directory.Exists(npath))
                        {
                            Directory.Delete(npath, true);
                        }
                        else
                        {
                            File.Delete(npath);
                        }
                    }
                    catch (Exception ex)
                    {
                        p.notError = false;
                        p.errorMsg += ex.ToString();
                    }
                }
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
    public class PostJSON
    {
        public bool isError { get; set; }
        public string errorMsg { get; set; }
        public object obj { get; set; }
        public int leng { get; set; }//不能为length?
        public bool notError { get; set; }

        public PostJSON()
        {
            notError = true;
        }

        /// <summary>
        /// 得到该对象的json字符串
        /// </summary>
        /// <returns></returns>
        public string getJson()
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            return jss.Serialize(this);
        }
    }
}