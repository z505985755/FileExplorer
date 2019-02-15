using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace FileExplorer
{
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