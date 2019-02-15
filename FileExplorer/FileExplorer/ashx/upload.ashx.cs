using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Script.Serialization;
using System.Configuration;

namespace FileExplorer
{
    /// <summary>
    /// upload 的摘要说明
    /// </summary>
    public class upload : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            string fileRootPath = ConfigurationManager.AppSettings["filePath"];
            string path = context.Request["path"];//要上传的路径
            string fileTemp = ConfigurationManager.AppSettings["fileTemp"];
            HttpPostedFile hpf = context.Request.Files[0];
            int chunk = Convert.ToInt32(context.Request.Form["chunk"]);//当前分片在上传分片中的顺序（从0开始）
            int chunks = Convert.ToInt32(context.Request.Form["chunks"]);//总分片数
            string guid = context.Request.Form["guid"];//guid
            //根据GUID创建用该GUID命名的临时文件夹
            string newPath = "";
            string res = "";
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var fileLength = hpf.InputStream.Length;
            if (chunks>1)//分段上传
            {
                newPath = fileTemp + "\\" + path + "\\" + hpf.FileName;
                Directory.CreateDirectory(newPath);
                newPath += "\\" + chunk;
                res = "{\"chunked\" : true,\"path\":\"" + path + "\" ,\"hasError\" : false, \"fileName\" : \"" + hpf.FileName + "\"}";
            }
            else
            {
                newPath = fileRootPath + "\\" + path + "\\";
                newPath = newPath + fileBLL.getNotRepeatFileName(newPath, hpf.FileName, 1);
                res = "{\"chunked\" : false, \"hasError\" : false,\"path\":\"" + path + "\"}";
            }
            hpf.SaveAs(newPath);

            context.Response.Write(res);
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