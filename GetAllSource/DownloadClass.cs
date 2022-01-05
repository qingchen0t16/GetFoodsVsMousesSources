using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace GetAllSource
{
    public static class DownloadClass
    {
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="filePath">存放位置</param>
        /// <param name="action">完成后执行内容</param>
        /// <param name="errerMsg">错误显示内容</param>
        /// <returns></returns>
        public static bool DownloadFile(string url, string filePath, Action<float, string> action, string errerMsg = "null")
        {
            float percent = 0;
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                long totalBytes = response.ContentLength;
                Stream st = response.GetResponseStream();
                Stream fileSt = new FileStream(filePath, FileMode.Create);
                long totalDownloadBytes = 0;
                byte[] by = new byte[1024];
                int osize = st.Read(by, 0, by.Length);
                while (osize > 0)
                {
                    totalDownloadBytes += osize;
                    fileSt.Write(by, 0, osize);
                    osize = st.Read(by, 0, by.Length);

                    percent = (float)totalDownloadBytes / totalBytes * 100F;
                    if (action != null)
                        action(percent, "");
                }
                st.Close();
                fileSt.Close();
                return true;
            }
            catch (Exception ex)
            {
                if (action != null)
                    action(0, errerMsg == "null" ? ex.Message : errerMsg);
                return false;
            }
        }
    }
}
