using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Checksums;
using System.Text;
using System.Configuration;

namespace FileExplorer
{
    /// <summary>
    /// downloadFiles 的摘要说明
    /// </summary>
    public class downloadFiles : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            string fileRootPath = ConfigurationManager.AppSettings["filePath"];
            string fileTemp = ConfigurationManager.AppSettings["fileTemp"];
            string path = context.Request["path"];
            string names = context.Request["names"];
            string fileName = "";
            string[] nameArr = names.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            string RealFile = fileRootPath + "\\" + path + "\\" + nameArr[0];
            if (nameArr.Length == 1 && System.IO.File.Exists(RealFile))//如果下载文件只有一个并且不是文件夹
            {
                fileName = nameArr[0];
            }
            else//如果有多个则压缩
            {
                if (nameArr.Length == 1)
                {
                    fileName = nameArr[0] + ".zip";
                    RealFile = fileTemp + fileName;
                    List<string> stra = new List<string>();
                    stra.Add(fileRootPath + "\\" + path + "\\" + nameArr[0]);
                    ZipHelper.Compress(stra, RealFile);
                }
                else
                {
                    fileName = nameArr[0] + "等" + nameArr.Length + "个文件.zip";
                    string guid = Guid.NewGuid().ToString();
                    RealFile = fileTemp + guid;
                    Directory.CreateDirectory(RealFile + "1");
                    List<string> stra = new List<string>();
                    for (int i = 0; i < nameArr.Length; i++)
                    {
                        stra.Add(fileRootPath + path + "\\" + nameArr[i]);
                    }
                    ZipHelper.Compress(stra, RealFile);
                    Directory.Delete(RealFile + "1", true);
                    //File.Delete(RealFile);
                }

            }
            if (!System.IO.File.Exists(RealFile))
            {
                context.Response.Write("服务器上该文件已被删除或不存在！'"); return;
            }
            long startPos = 0;
            FileInfo fi = new FileInfo(RealFile); 
            //所传输的文件长度 
            long fileTranLen = fi.Length;

            //断点续传请求 
            if (context.Request.Headers["Range"] != null)
            {
                context.Response.StatusCode = 206;
                startPos = long.Parse(context.Request.Headers["Range"].Replace("bytes=", "").Split('-')[0]);
                fileTranLen -= startPos;

                //Response.AddHeader("Accept-Ranges", "bytes"); 
                //Content-Range: bytes [文件块的开始字节]-[传输文件的总大小]/[文件的总大小] 
                context.Response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", startPos, fileTranLen, fi.Length));
            }

            context.Response.AddHeader("Content-Length", fileTranLen.ToString());

            //基本的文件下载报文头 
            context.Response.ContentType = "application/octet-stream";
            if (context.Request.UserAgent.ToLower().IndexOf("msie") > -1)
            {
                //当客户端使用IE时，对其进行编码；We should encode the filename when our visitors use IE
                //使用 ToHexString 代替传统的 UrlEncode()；We use "ToHexString" replaced "context.Server.UrlEncode(fileName)"
                fileName = ToHexString(fileName);
            }
            if (context.Request.UserAgent.ToLower().IndexOf("firefox") > -1)
            {
                //为了向客户端输出空格，需要在当客户端使用 Firefox 时特殊处理
                //we should do some special work whem our visitor has a firefox browser
                context.Response.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
            }
            else
                context.Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);

            //简单的流拷贝 
            using (System.IO.Stream fileStream = System.IO.File.OpenRead(RealFile))
            {
                fileStream.Position = startPos;

                byte[] buffer = new Byte[102400];
                int count;
                while ((count = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    context.Response.OutputStream.Write(buffer, 0, count);
                    context.Response.Flush();
                }
                fileStream.Dispose();
                context.Response.OutputStream.Dispose();
                context.Response.Close();
                context.Response.End();
            }
            
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
        #region 编码

        /// <summary>
        /// Encodes non-US-ASCII characters in a string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToHexString(string s)
        {
            char[] chars = s.ToCharArray();
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < chars.Length; index++)
            {
                bool needToEncode = NeedToEncode(chars[index]);
                if (needToEncode)
                {
                    string encodedString = ToHexString(chars[index]);
                    builder.Append(encodedString);
                }
                else
                {
                    builder.Append(chars[index]);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Determines if the character needs to be encoded.
        /// </summary>
        /// <param name="chr"></param>
        /// <returns></returns>
        private static bool NeedToEncode(char chr)
        {
            string reservedChars = "$-_.+!*'(),@=&";

            if (chr > 127)
                return true;
            if (char.IsLetterOrDigit(chr) || reservedChars.IndexOf(chr) >= 0)
                return false;

            return true;
        }

        /// <summary>
        /// Encodes a non-US-ASCII character.
        /// </summary>
        /// <param name="chr"></param>
        /// <returns></returns>
        private static string ToHexString(char chr)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] encodedBytes = utf8.GetBytes(chr.ToString());
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < encodedBytes.Length; index++)
            {
                builder.AppendFormat("%{0}", Convert.ToString(encodedBytes[index], 16));
            }

            return builder.ToString();
        }


        #endregion
        /// <summary>
        /// 根据文件后缀来获取MIME类型字符串
        /// </summary>
        /// <param name="extension">文件后缀</param>
        /// <returns></returns>
        static string GetMimeType(string extension)
        {
            string mime = string.Empty;
            extension = extension.ToLower();
            switch (extension)
            {
                case ".avi": mime = "video/x-msvideo"; break;
                case ".bin":
                case ".exe":
                case ".msi":
                case ".dll":
                case ".class": mime = "application/octet-stream"; break;
                case ".csv": mime = "text/comma-separated-values"; break;
                case ".html":
                case ".htm":
                case ".shtml": mime = "text/html"; break;
                case ".css": mime = "text/css"; break;
                case ".js": mime = "text/javascript"; break;
                case ".doc":
                case ".dot":
                case ".docx": mime = "application/msword"; break;
                case ".xla":
                case ".xls":
                case ".xlsx": mime = "application/msexcel"; break;
                case ".ppt":
                case ".pptx": mime = "application/mspowerpoint"; break;
                case ".gz": mime = "application/gzip"; break;
                case ".gif": mime = "image/gif"; break;
                case ".bmp": mime = "image/bmp"; break;
                case ".jpeg":
                case ".jpg":
                case ".jpe":
                case ".png": mime = "image/jpeg"; break;
                case ".mpeg":
                case ".mpg":
                case ".mpe":
                case ".wmv": mime = "video/mpeg"; break;
                case ".mp3":
                case ".wma": mime = "audio/mpeg"; break;
                case ".pdf": mime = "application/pdf"; break;
                case ".rar": mime = "application/octet-stream"; break;
                case ".txt": mime = "text/plain"; break;
                case ".7z":
                case ".z": mime = "application/x-compress"; break;
                case ".zip": mime = "application/x-zip-compressed"; break;
                default:
                    mime = "application/octet-stream";
                    break;
            }
            return mime;
        }
    }
    public class ZipHelper
    {
        /// <summary>
        /// 递归压缩文件
        /// </summary>
        /// <param name="sourceFilePath">待压缩的文件或文件夹路径</param>
        /// <param name="zipStream">打包结果的zip文件路径（类似 D:\WorkSpace\a.zip）,全路径包括文件名和.zip扩展名</param>
        /// <param name="staticFile"></param>
        private static void CreateZipFiles(string sourceFilePath, ZipOutputStream zipStream, string path)
        {
            //先判断当前是文件夹还是文件
            //对当前文件夹的文件夹遍历并递归调用该方法
            //对当前文件夹下的文件遍历并递归
            //如果是文件直接压缩了事
            if (Directory.Exists(sourceFilePath))
            {
                DirectoryInfo di = new DirectoryInfo(sourceFilePath);
                DirectoryInfo[] dis = di.GetDirectories();
                FileInfo[] fis = di.GetFiles();
                if (string.IsNullOrEmpty(path))//如果是顶层文件,则加入顶层文件的名称
                {
                    path = di.Name;
                }
                foreach (var item in dis)
                {
                    CreateZipFiles(item.FullName, zipStream, path + "\\" + item.Name);
                }
                foreach (var item in fis)
                {
                    CreateZipFiles(item.FullName, zipStream, path + "\\");
                }
            }
            else
            {
                Crc32 crc = new Crc32();
                FileStream fileStream = File.OpenRead(sourceFilePath);
                byte[] buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
                string tempFile = path + sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1);//保留文件夹结构的关键点
                ZipEntry entry = new ZipEntry(tempFile);
                entry.DateTime = DateTime.Now;
                entry.Size = fileStream.Length;
                fileStream.Close();
                crc.Reset();
                crc.Update(buffer);
                entry.Crc = crc.Value;
                zipStream.PutNextEntry(entry);
                zipStream.Write(buffer, 0, buffer.Length);
            }
        }
        public static void Compress(List<string> sources, string TartgetFile)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TartgetFile));
            using (ZipOutputStream s = new ZipOutputStream(File.Create(TartgetFile)))
            {
                s.SetLevel(6);
                foreach (var item in sources)
                {
                    CreateZipFiles(item, s, "");
                }
                s.Finish();
                s.Close();
            }
        }
    }
}