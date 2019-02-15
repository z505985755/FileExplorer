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
    /// getUserFolder 的摘要说明
    /// </summary>
    public class getUserFolder : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            string order = context.Request["order"];
            string fileRootPath = ConfigurationManager.AppSettings["filePath"];
            string path = context.Request["path"];
            string search = context.Request["search"];
            string rootPath = fileRootPath;
            path = rootPath + "\\" + path;
            PostJSON p = new PostJSON();
            JavaScriptSerializer jss = new JavaScriptSerializer();
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
}