using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

/// <summary>
/// readUserFileHandler 的摘要说明
/// </summary>
public class readUserFileHandler : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/octet-stream";
        string fileRootPath = ConfigurationManager.AppSettings["filePath"];
        string filePath = context.Server.UrlDecode(context.Request.Url.AbsolutePath);
        int index = filePath.LastIndexOf(".preview");
        filePath = filePath.Remove(index);
        filePath = fileRootPath + "/" + filePath;
        context.Response.WriteFile(filePath);
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