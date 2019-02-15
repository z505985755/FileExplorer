using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Configuration;

namespace FileExplorer
{
    /// <summary>
    /// mergeFile 的摘要说明
    /// </summary>
    public class mergeFile : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            string fileRootPath = ConfigurationManager.AppSettings["filePath"];
            string fileTemp = ConfigurationManager.AppSettings["fileTemp"];
            string path = context.Request["path"];//要上传的路径
            string fileName = context.Request["fileName"];
            string newPath = fileRootPath + "\\" + path + "\\";
            newPath = newPath + fileBLL.getNotRepeatFileName(newPath, fileName, 1);
            string oldPath = fileTemp + "\\" + path + "\\" + fileName;
            FileStream fs = File.Create(oldPath+1);
            fs.Dispose();
            DirectoryInfo di = new DirectoryInfo(oldPath);
            FileInfo[] fis = di.GetFiles();
            foreach (var item in fis)
            {
                FileStream addFile = new FileStream(oldPath + 1, FileMode.Append, FileAccess.Write);
                BinaryWriter AddWriter = new BinaryWriter(addFile);
                Stream stream = new FileStream(item.FullName, FileMode.Open);
                BinaryReader TempReader = new BinaryReader(stream);
                //将分片追加到临时文件末尾
                AddWriter.Write(TempReader.ReadBytes((int)stream.Length));
                //关闭BinaryReader文件阅读器
                TempReader.Close();
                stream.Close();
                TempReader.Dispose();
                stream.Dispose();
                AddWriter.Close();
                addFile.Close();
                AddWriter.Dispose();
                addFile.Dispose();
            }
            Directory.Delete(oldPath, true);//删除源文件
            fileBLL.move(oldPath + 1, newPath);
            string res = "{\"hasError\" : false}";
            context.Response.Write(res);
            context.Response.End();
            //这个算法需要大量内存,还需优化
            //优化为这个算法需要大量倒腾内存,仍需优化
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