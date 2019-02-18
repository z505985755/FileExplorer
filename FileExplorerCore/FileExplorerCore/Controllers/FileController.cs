using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FileExplorerCore.Controllers
{
    public class FileController : Controller
    {
        public string rootPath
        {
            get
            {
                var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
                var configurationRoot = builder.Build();
                return configurationRoot.GetSection("filePath").Value.ToString();
            }
        }
        public string fileTemp
        {
            get
            {
                var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
                var configurationRoot = builder.Build();
                return configurationRoot.GetSection("fileTemp").Value.ToString();
            }
        }
        public IActionResult Index()
        {
            return Json("index");
        }
        public IActionResult GetUserFolder(string order, string path)
        {
            path = rootPath + "\\" + path;
            PostJSON p = new PostJSON();
            if (!Directory.Exists(rootPath))//如果用户根路径不存在则创建
            {
                Directory.CreateDirectory(rootPath);
            }
            if (path.IndexOf("..\\") == -1 && path.IndexOf("../") == -1)//如果包含上一级符号则屏蔽
            {
                if (Directory.Exists(path))
                {
                    folder folder = new folder();
                    folder = fileBLL.getFloder(path);
                    switch (order)
                    {
                        case "order_nameascending": folder.sortByName(); break;
                        case "order_sizeascending": folder.sortBySize(); break;
                        case "order_ModifiedDateascending": folder.sortByModifiedDate(); break;
                        case "order_CreatDateTimeascending": folder.sortByCreatDateTime(); break;
                        case "order_namedesc": folder.sortByNameDesc(); break;
                        case "order_sizedesc": folder.sortBySizeDesc(); break;
                        case "order_ModifiedDatedesc": folder.sortByModifiedDateDesc(); break;
                        case "order_CreatDateTimedesc": folder.sortByCreatDateTimeDesc(); break;
                        default:
                            break;
                    }
                    p.obj = folder;
                }
                else
                {
                    p.isError = true;
                    p.errorMsg = "指定的目录不存在!";
                }
            }
            else
            {
                p.isError = false;
                p.errorMsg = "不允许使用上一级符号";
            }
            return Json(p);
        }
        public IActionResult DelFile(string names, string path)
        {
            path = rootPath + "\\" + path;
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
                            System.IO.File.Delete(npath);
                        }
                    }
                    catch (Exception ex)
                    {
                        p.notError = false;
                        p.errorMsg += ex.ToString();
                    }
                }
            }
            return Json(p);
        }
        public IActionResult NewFolder(string path)
        {
            path = rootPath + "\\" + path;
            //postJSON p = new postJSON();
            string fplderName = fileBLL.createFolder(path, 1).Name;
            //p.isError = false;
            //p.obj = fplderName;
            return Json(fplderName);
        }
        public IActionResult Rename(string newName, string path, string oldName)
        {
            string newPath = rootPath + "\\" + path + "\\" + newName;//原路径
            string oldPath = rootPath + path + "\\" + oldName;//原路径
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

            return Json(p);
        }
        public IActionResult StickFiles(string names, string oldPath, string newPath, bool isCopy)
        {
            string[] nameArr = names.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            oldPath = rootPath + "\\" + oldPath + "\\";
            oldPath = oldPath.Replace("/", "\\").Replace(@"\\", "\\");
            newPath = rootPath + "\\" + newPath + "\\";
            newPath = newPath.Replace("/", "\\").Replace(@"\\", "\\");
            if (isCopy)
            {
                long filesLength = 0;
                foreach (var item in nameArr)
                {
                    if (System.IO.File.Exists(oldPath + item))
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

            return Json("{\"error\":false}");
        }
        public IActionResult Upload(string path, int chunk, int chunks, string guid, List<IFormFile> file)
        {
            //根据GUID创建用该GUID命名的临时文件夹
            string newPath = "";
            string res = "";
            var fileLength = file[0].Length;
            if (chunks > 1)//分段上传
            {
                newPath = fileTemp + "\\" + path + "\\" + file[0].FileName;
                Directory.CreateDirectory(newPath);
                newPath += "\\" + chunk;
                res = "{\"chunked\" : true,\"path\":\"" + path + "\" ,\"hasError\" : false, \"fileName\" : \"" + file[0].FileName + "\"}";
            }
            else
            {
                newPath = rootPath + "\\" + path + "\\";
                newPath = newPath + fileBLL.getNotRepeatFileName(newPath, file[0].FileName, 1);
                res = "{\"chunked\" : false, \"hasError\" : false,\"path\":\"" + path + "\"}";
            }
            using (var stream = new FileStream(newPath, FileMode.Create))
            {

                file[0].CopyToAsync(stream);
            }
            return Json(res);
        }
        public IActionResult MergeFile(string path, string fileName)
        {
            string newPath = rootPath + "\\" + path + "\\";
            newPath = newPath + fileBLL.getNotRepeatFileName(newPath, fileName, 1);
            string oldPath = fileTemp + "\\" + path + "\\" + fileName;
            FileStream fs = System.IO.File.Create(oldPath + 1);
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
            return Json(res);
        }
        public IActionResult DownloadFiles(string path, string names)
        {
            path = System.Web.HttpUtility.UrlDecode(path);
            names = System.Web.HttpUtility.UrlDecode(names);
            string fileName = "";
            string[] nameArr = names.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            string RealFile = rootPath + "\\" + path + "\\" + nameArr[0];
            if (nameArr.Length == 1 && System.IO.File.Exists(RealFile))//如果下载文件只有一个并且不是文件夹
            {
                fileName = nameArr[0];
            }
            else//如果有多个则压缩
            {
                if (nameArr.Length == 1)
                {
                    fileName = nameArr[0] + ".zip";
                    RealFile = fileTemp + "\\" + fileName;
                    List<string> stra = new List<string>();
                    stra.Add(rootPath + "\\" + path + "\\" + nameArr[0]);
                    ZipHelper.Compress(stra, RealFile);
                }
                else
                {
                    fileName = nameArr[0] + "等" + nameArr.Length + "个文件.zip";
                    string guid = Guid.NewGuid().ToString();
                    RealFile = fileTemp + "\\" + guid;
                    Directory.CreateDirectory(RealFile + "1");
                    List<string> stra = new List<string>();
                    for (int i = 0; i < nameArr.Length; i++)
                    {
                        stra.Add(rootPath + path + "\\" + nameArr[i]);
                    }
                    ZipHelper.Compress(stra, RealFile);
                    Directory.Delete(RealFile + "1", true);
                    //File.Delete(RealFile);
                }

            }
            if (!System.IO.File.Exists(RealFile))
            {
                return Json("服务器上该文件已被删除或不存在！'");
            }
            //简单的流拷贝 
            FileStreamResult f; ;
            System.IO.Stream fileStream = System.IO.File.OpenRead(RealFile);
                f = File(fileStream, "application/octet-stream", fileName);
            return f;
        }
        public IActionResult Preview(string path)
        {
            byte[] filebytes = System.IO.File.ReadAllBytes(rootPath + "\\" + path);
            return File(filebytes, "image/jpeg");
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
    public class file
    {
        /// <summary>
        /// 文件名 
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 文件大小
        /// </summary>
        public long size { get; set; }
        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedDate { get; set; }
        public string ModifiedDate2 { get { return ModifiedDate.ToString(); } }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatDateTime { get; set; }
        public string CreatDateTime2 { get { return CreatDateTime.ToString(); } }
    }
    public class folder : file
    {
        /// <summary>
        /// 文件夹内所包含的文件
        /// </summary>
        public List<file> files { get; set; }
        /// <summary>
        /// 文件夹内所包含的文件夹
        /// </summary>
        public List<folder> folders { get; set; }
        /// <summary>
        /// 包含的文件夹个数
        /// </summary>
        public int folderLength { get; set; }
        /// <summary>
        /// 包含的文件个数
        /// </summary>
        public int fileLength { get; set; }
        /// <summary>
        /// 以自定义的方式排序folder
        /// </summary>
        /// <param name="comparison"></param>
        public void sort(Comparison<file> comparison)
        {
            this.files.Sort(comparison);
            this.folders.Sort(comparison);
        }
        public void sortByName()
        {
            sort((x, y) => x.name.CompareTo(y.name));
        }
        public void sortByNameDesc()
        {
            sort((x, y) => -x.name.CompareTo(y.name));
        }
        public void sortBySize()
        {
            sort((x, y) => x.size.CompareTo(y.size));
        }
        public void sortBySizeDesc()
        {
            sort((x, y) => -x.size.CompareTo(y.size));
        }
        public void sortByModifiedDate()
        {
            sort((x, y) => x.ModifiedDate.CompareTo(y.ModifiedDate));
        }
        public void sortByModifiedDateDesc()
        {
            sort((x, y) => -x.ModifiedDate.CompareTo(y.ModifiedDate));
        }
        public void sortByCreatDateTime()
        {
            sort((x, y) => x.CreatDateTime.CompareTo(y.CreatDateTime));
        }
        public void sortByCreatDateTimeDesc()
        {
            sort((x, y) => -x.CreatDateTime.CompareTo(y.CreatDateTime));
        }
    }
    public class fileBLL
    {
        /// <summary>
        /// 获取一个文件夹下所有文件和文件夹,包含子目录
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static folder getRootFloder(string path)
        {
            folder f = new folder();
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] fis = di.GetFiles();
            List<file> files = new List<file>();
            foreach (var item in fis)
            {
                file file = getFile(item);
                files.Add(file);
            }
            DirectoryInfo[] dis = di.GetDirectories();
            List<folder> fs = new List<folder>();
            foreach (var item in dis)
            {
                folder folder = getRootFloder(item);
                fs.Add(folder);
            }
            f.name = di.Name;
            f.ModifiedDate = di.LastWriteTime;
            f.files = files;
            f.folders = fs;
            return f;
        }
        public static folder getRootFloder(DirectoryInfo di)
        {
            folder f = new folder();
            FileInfo[] fis = di.GetFiles();
            List<file> files = new List<file>();
            foreach (var item in fis)
            {
                file file = getFile(item);
                files.Add(file);
            }
            DirectoryInfo[] dis = di.GetDirectories();
            List<folder> fs = new List<folder>();
            foreach (var item in dis)
            {
                folder folder = getRootFloder(item);
                fs.Add(folder);
            }
            f.name = di.Name;
            f.ModifiedDate = di.LastWriteTime;
            f.files = files;
            f.folders = fs;
            return f;
        }
        public static file getFile(string path)
        {
            file file = new file();
            FileInfo fi = new FileInfo(path);
            file.ModifiedDate = fi.LastWriteTime;
            file.CreatDateTime = fi.CreationTime;
            file.name = fi.Name;
            file.size = fi.Length;
            return file;
        }
        public static file getFile(FileInfo fi)
        {
            file file = new file();
            file.ModifiedDate = fi.LastWriteTime;
            file.CreatDateTime = fi.CreationTime;
            file.name = fi.Name;
            file.size = fi.Length;
            return file;
        }
        public static void createFile(string path, byte[] bytes)
        {
            System.IO.File.WriteAllBytes(path, bytes);
        }
        public static void createFile(string path, string content)
        {
            System.IO.File.WriteAllText(path, content);
        }
        public static DirectoryInfo createFolder(string path, int i)//新建文件夹时像windows那样自动再后面加(i);
        {
            string path2 = path;
            if (i == 1)//第一次创建
            {
                path += "\\新建文件夹";
            }
            else
            {
                path += "\\新建文件夹 (" + i + ")";
            }
            if (Directory.Exists(path))//存在则再文件后加(2);
            {
                return createFolder(path2, i + 1);
            }
            else//不存在则创建
            {
                return System.IO.Directory.CreateDirectory(path);
            }
        }
        /// <summary>
        /// 获取文件夹下所有文件和文件夹,不包含子目录
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static folder getFloder(string path)
        {
            folder f = new folder();
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] fis = di.GetFiles();
            List<file> files = new List<file>();
            foreach (var item in fis)
            {
                file file = getFile(item);
                files.Add(file);
            }
            DirectoryInfo[] dis = di.GetDirectories();
            List<folder> fs = new List<folder>();
            foreach (var item in dis)
            {
                folder folder = new folder();
                folder.ModifiedDate = item.LastWriteTime;
                folder.CreatDateTime = item.CreationTime;
                folder.name = item.Name;
                folder.fileLength = getFileLength(item.FullName);
                folder.folderLength = getFolderLength(item.FullName);
                folder.size = getFolderSize(item.FullName);
                fs.Add(folder);
            }
            f.name = di.Name;
            f.ModifiedDate = di.LastWriteTime;
            f.files = files;
            f.folders = fs;
            f.CreatDateTime = di.CreationTime;
            f.fileLength = getFileLength(di.FullName);
            f.folderLength = getFolderLength(di.FullName);
            f.size = getFolderSize(di.FullName);
            return f;
        }
        public static folder getFloder(DirectoryInfo di)
        {
            folder f = new folder();
            FileInfo[] fis = di.GetFiles();
            List<file> files = new List<file>();
            foreach (var item in fis)
            {
                file file = getFile(item);
                files.Add(file);
            }
            DirectoryInfo[] dis = di.GetDirectories();
            List<folder> fs = new List<folder>();
            foreach (var item in dis)
            {
                folder folder = new folder();
                folder.ModifiedDate = item.LastWriteTime;
                folder.CreatDateTime = item.CreationTime;
                folder.name = item.Name;
                folder.fileLength = getFileLength(item.FullName);
                folder.folderLength = getFolderLength(item.FullName);
                folder.size = getFolderSize(item.FullName);
                fs.Add(folder);
            }
            f.name = di.Name;
            f.ModifiedDate = di.LastWriteTime;
            f.files = files;
            f.folders = fs;
            f.CreatDateTime = di.CreationTime;
            f.fileLength = getFileLength(di.FullName);
            f.folderLength = getFolderLength(di.FullName);
            f.size = getFolderSize(di.FullName);
            return f;
        }
        /// <summary>
        /// 该方法用来生成一个不重名文件夹的名字
        /// </summary>
        /// <param name="path">不包含文件名的路径</param>
        /// <param name="name"></param>
        /// <param name="i">调用时该参数为1</param>
        /// <returns></returns>
        public static string getNotRepeatFolderName(string path, string name, int i)
        {
            string newname = name;
            if (i > 1)
            {
                name += " (" + i + ")";
            }
            if (Directory.Exists(path + "\\" + name) || File.Exists(path + "\\" + name))
            {
                return getNotRepeatFolderName(path, newname, i + 1);
            }
            else
            {
                return name;
            }
        }
        /// <summary>
        /// 该方法用来生成一个不重名文件的名字
        /// </summary>
        /// <param name="path">不包含文件名的路径</param>
        /// <param name="name"></param>
        /// <param name="i">调用时该参数为1</param>
        /// <returns></returns>
        public static string getNotRepeatFileName(string path, string name, int i)
        {
            string newname = name;
            if (i > 1)
            {
                int index = name.LastIndexOf(".");
                if (index > -1)//有文件后缀
                {
                    string ex = name.Substring(index);//文件后缀
                    name = name.Substring(0, name.LastIndexOf(".")) + " (" + i + ")" + ex;
                }
                else
                {
                    name += " (" + i + ")";
                }
            }
            if (Directory.Exists(path + "\\" + name) || File.Exists(path + "\\" + name))
            {
                return getNotRepeatFileName(path, newname, i + 1);
            }
            else
            {
                return name;
            }
        }
        /// <summary>
        /// 递归复制文件夹,使用该方法应避免将一个父文件夹复制到子文件夹内,否则将会无穷递归,最终将因为路径过长而终止,
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="destFileName"></param>
        /// <returns></returns>
        public static void copyFiles(string sourceFileName, string destFileName)
        {
            if (Directory.Exists(sourceFileName))//是否为文件夹
            {
                DirectoryInfo di = new DirectoryInfo(sourceFileName);
                DirectoryInfo[] dis = di.GetDirectories();
                FileInfo[] fis = di.GetFiles();
                Directory.CreateDirectory(destFileName);//若目标文件夹已存在,则自动合并
                foreach (var item in dis)
                {
                    string thisPath = item.FullName;
                    copyFiles(thisPath, destFileName + thisPath.Substring(di.FullName.Length));//是文件夹再次调用该函数
                }
                foreach (var item in fis)//对文件夹内不是文件夹的文件复制了事
                {
                    string thisPath = item.FullName;
                    copyFile(thisPath, destFileName + thisPath.Substring(di.FullName.Length));
                }
            }
            else//不是文件夹直接复制了事
            {
                copyFile(sourceFileName, destFileName);
            }
        }
        /// <summary>
        /// 复制文件,遇到重名文件自动更名
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="destFileName"></param>
        /// <returns></returns>
        public static void copyFile(string sourceFileName, string destFileName)
        {
            sourceFileName = sourceFileName.Replace("/", "\\");
            destFileName = destFileName.Replace("/", "\\");
            string fileName = destFileName.Substring(destFileName.LastIndexOf("\\"));
            string path = destFileName.Substring(0, destFileName.LastIndexOf("\\"));
            if (Directory.Exists(sourceFileName))//若目标文件夹已存在,则自动合并,当原路径和目标路径一样时有bug,
            {
                fileName = getNotRepeatFolderName(path, fileName, 1);
            }
            else
            {
                fileName = getNotRepeatFileName(path, fileName, 1);
            }
            File.Copy(sourceFileName, path + fileName);
        }
        public static void move(string sourceFileName, string destFileName)
        {
            sourceFileName = sourceFileName.Replace("/", "\\");
            destFileName = destFileName.Replace("/", "\\");
            string fileName = destFileName.Substring(destFileName.LastIndexOf("\\"));
            string path = destFileName.Substring(0, destFileName.LastIndexOf("\\"));
            if (Directory.Exists(sourceFileName))
            {
                fileName = getNotRepeatFolderName(path, fileName, 1);
            }
            else
            {
                fileName = getNotRepeatFileName(path, fileName, 1);
            }
            Directory.Move(sourceFileName, path + fileName);
        }
        public static int getFileLength(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            return di.GetFiles("*", SearchOption.AllDirectories).Length;
        }
        public static int getFolderLength(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            return di.GetDirectories("*", SearchOption.AllDirectories).Length;
        }
        public static long getFolderSize(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            long size = 0;
            FileInfo[] fis = di.GetFiles("*", SearchOption.AllDirectories);
            foreach (var item in fis)
            {
                size += item.Length;
            }
            return size;
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