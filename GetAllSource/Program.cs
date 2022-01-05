using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;

namespace GetAllSource
{
    /// <summary>
    /// 线程获取资源类
    /// </summary>
    public class ThdDown
    {
        /// <summary>
        /// 资源信息
        /// </summary>
        public class ThdConfig
        {
            public string Path, // 存放地址
                          BaseUrl,  // 文件地址
                          FileName, // 文件名称
                          FileType, // 文件类型
                          FilePathName, // 文件在路径的名称
                          SourceID;    // 资源ID

            public ThdConfig(string path, string baseUrl, string fileName, string filePathName, string fileType, string sourceID)
            {
                Path = path;
                BaseUrl = baseUrl;
                FileName = fileName;
                FilePathName = filePathName;
                FileType = fileType;
                SourceID = sourceID;
            }

            /// <summary>
            /// 获取Sources后的文件所处根目录的相对路径
            /// </summary>
            /// <returns></returns>
            public string GetRootPath() => Path.Substring(0, Path.IndexOf('/') + 1);
            /// <summary>
            /// 获取Sources后的文件处在的相对路径
            /// </summary>
            /// <returns></returns>
            public string GetFilePath() => Path.Substring(0, Path.LastIndexOf('/') + 1);
            /// <summary>
            /// 获取携带Sources目录的文件处在的相对路径
            /// </summary>
            /// <returns></returns>
            public string GetTruePath() => "Sources/" + GetFilePath();
            /// <summary>
            /// 获取携带Sources目录的文件的相对路径
            /// </summary>
            /// <returns></returns>
            public string GetFileTruePath() => "Sources/" + Path;
        }

        /// <summary>
        /// 资源下载配置
        /// </summary>
        private class ThdDownloader
        {
            public ThdConfig Config;
            public Action<ThdConfig> DoneAc, ExistsAc, FildAc;
        }

        // 所有配置记录
        List<ThdDownloader> downloads = new List<ThdDownloader>();
        /*
            ErrorFileNum    错误的文件数
            FileDoneNum     完成的文件数
            FileExistsNum      已存在文件
            AllFileNum      文件总数
            MaxTdNum        最大线程数
        */
        public int ErrorFileNum = 0,
                   FileDoneNum = 0,
                   FileExistsNum = 0,
                   AllFileNum = 0,
                   MaxTdNum;
        private Action DoneAc;
        private int executionsNum = 0;  // 已处理个数
        public static List<string> IdList = new List<string>();
        public double Percent { get => Math.Round(((float)executionsNum) / downloads.Count * 100, 2) > 100 ? 100 : Math.Round(((float)executionsNum) / downloads.Count * 100, 2); }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="thdNum">下载线程数</param>
        public ThdDown(int thdNum) => MaxTdNum = thdNum;

        /// <summary>
        /// 增加任务
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filePathName"></param>
        /// <param name="fileName"></param>
        /// <param name="doneAc"></param>
        /// <param name="fildAc"></param>
        public void Push(string id, string url, string path, string filePathName, string fileName, Action<ThdConfig> confDoneAc, Action<ThdConfig> confExistsAc, Action<ThdConfig> confFildAc, string fileType = "数据文件")
        {
            AllFileNum++;
            Directory.CreateDirectory("Sources/" + path);    // 创建文件夹
            // 添加下载请求
            downloads.Add(new ThdDownloader
            {
                Config = new ThdConfig(path + filePathName, url, fileName, filePathName, fileType, id),
                DoneAc = confDoneAc,
                ExistsAc = confExistsAc,
                FildAc = confFildAc
            });
        }

        /// <summary>
        /// 开始执行任务
        /// </summary>
        /// <param name="doneAc">完成后执行的Action</param>
        public void Start(Action doneAc)
        {
            DoneAc = doneAc;
            for (int i = 0; i < (AllFileNum < MaxTdNum ? AllFileNum : MaxTdNum); i++)
            {
                Thread thread = new Thread(Download);
                thread.Start(executionsNum++);
            }
        }

        /// <summary>
        /// 下载方法
        /// </summary>
        /// <param name="obj">index</param>
        public void Download(Object obj)
        {
            int index = (int)obj;
            if (index >= AllFileNum)
                return;
            // 文件是否已存在
            if (new FileInfo(downloads[index].Config.GetFileTruePath()).Exists || IdList.Contains(downloads[index].Config.SourceID))
            {
                downloads[index].ExistsAc(downloads[index].Config);
                FileExistsNum++;
            }
            else if (!DownloadClass.DownloadFile(downloads[index].Config.BaseUrl,
                        downloads[index].Config.GetFileTruePath(), null))
            {
                downloads[index].FildAc(downloads[index].Config);
                ErrorFileNum++;
            }
            else
            {
                downloads[index].DoneAc(downloads[index].Config);
                FileDoneNum++;
                IdList.Add(downloads[index].Config.SourceID);
            }
            // 确保最后一个也下载下来了才执行DoneAc();
            executionsNum = AllFileNum < executionsNum ? AllFileNum : executionsNum;
            if (executionsNum == AllFileNum && FileDoneNum + FileExistsNum + ErrorFileNum == AllFileNum)
                DoneAc();
            Console.Title = ProTextSpan();
            Download(executionsNum++);
        }

        /// <summary>
        /// 显示进度文本
        /// </summary>
        /// <returns></returns>
        private string ProTextSpan() {
            int num = Convert.ToInt32(Math.Round(Percent / 10));
            string temp = "";
            for (int i = 0; i < num; i++)
                temp += "=";
            for (int i = 0; i < 10 - num; i++)
                temp += "  ";
            return $"{Percent}% [| {temp} |]";
        }

        /// <summary>
        /// 委托下载请求
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="url"></param>
        /// <param name="doneAc"></param>
        /// <param name="fildAc"></param>
        public void DownloadInvoke(ThdConfig conf, string url, Action<ThdConfig> doneAc, Action<ThdConfig> fildAc)
        {
            if (!DownloadClass.DownloadFile(url,
                        conf.GetFileTruePath(), null))
                fildAc(conf);
            else
                doneAc(conf);
        }
    }
    class Program
    {
        public static ThdDown NeedFile_ThdDown = new ThdDown(10), // 需求文件的ThdDown
                              SourceFile_ThdDown = new ThdDown(10);   // 资源文件的ThdDown
        static void Main(string[] args)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"获取XML文件...");

                // 获取LoadFilesList.xml(这个文件是直接用的美食官网的)
                Stream st = WebRequest.Create("https://q.ms.huanlecdn.com/4399/cdn.123u.com/LoadFilesList.xml").GetResponse().GetResponseStream();
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(new StreamReader(st).ReadToEnd());

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"正在获取全部需求文件...(线程数:{NeedFile_ThdDown.MaxTdNum})");

                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    string baseurl = node.Attributes["baseurl"].InnerText;
                    foreach (XmlNode _node in node.ChildNodes)
                        NeedFile_ThdDown.Push(
                            _node.Attributes["url"].InnerText,
                            $"https://q.ms.huanlecdn.com/4399/cdn.123u.com/{baseurl}{_node.Attributes["url"].InnerText}",
                            "FileList/" + node.Attributes["baseurl"].InnerText,
                            _node.Attributes["url"].InnerText,
                            _node.Attributes["url"].InnerText,
                            (config) =>
                            {
                                // 下载成功代码
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"({NeedFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载完成");
                                string fileName = config.FilePathName.Substring(config.FilePathName.LastIndexOf(".") + 1);
                                // FVM的压缩文件后缀是 .dat 或者 .txt
                                if (fileName == "dat" || fileName == "txt") // 判断是否为FVM的压缩文件
                                {
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    Console.WriteLine($"[{config.FileType}] :{config.FileName} 开始解压");
                                    // 创建解压路径
                                    string unpackDic = "Sources/UnpackThe/" + config.GetFilePath() + config.FilePathName.Substring(0, config.FilePathName.LastIndexOf("."));
                                    Directory.CreateDirectory(unpackDic);
                                    // 解压文件
                                    ZipFile.ExtractToDirectory(config.GetFileTruePath(), unpackDic);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"[{config.FileType}] :{config.FileName} 解压完成");
                                }
                            },
                            (config) =>
                            {
                                // 文件存在代码
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"({NeedFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 已存在");
                            },
                            (config) =>
                            {
                                // 下载失败代码
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"({NeedFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载失败 但是程序还会继续运行 去提取下一个文件");
                            },
                            "需求文件"
                            );
                }

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"获取到需求文件{NeedFile_ThdDown.AllFileNum}个,两秒后开始下载...");
                Thread.Sleep(2000);
                NeedFile_ThdDown.Start(() =>
                {
                    // 需求文件全部下载完毕
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"所有需求文件下载成功,其中 成功{NeedFile_ThdDown.FileDoneNum}个,失败{NeedFile_ThdDown.ErrorFileNum}个,文件已存在{NeedFile_ThdDown.FileExistsNum}个,两秒后开始解析资源...");
                    Thread.Sleep(2000);

                    // 开始解析PreLoadFilesList.xml
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"正在获取PreLoadFilesList...");
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load("Sources/FileList/PreLoadFilesList.xml");

                    string rootDec = "DownLoadFile/PreLoadFilesList/";
                    foreach (XmlNode node1 in xDoc.DocumentElement.ChildNodes)
                    {
                        string baseUrl = node1.Attributes["baseurl"].InnerText;
                        foreach (XmlNode node2 in node1.ChildNodes)
                        {
                            string n2Dec = rootDec + node2.Name;
                            foreach (XmlNode node3 in node2.ChildNodes)
                            {

                                string n3Url = node3.Attributes["url"].InnerText;
                                string n3Dec = n2Dec + "/" + baseUrl + (n3Url.LastIndexOf('/') == -1 ? "/" : n3Url.Substring(0, n3Url.LastIndexOf('/') + 1));

                                SourceFile_ThdDown.Push(
                                    node3.Attributes["name"].InnerText,
                                    $"https://q.ms.huanlecdn.com/4399/cdn.123u.com/{baseUrl}{n3Url}",
                                    n3Dec,
                                    node3.Attributes["name"].InnerText + n3Url.Substring(n3Url.LastIndexOf('.')),
                                    node3.Attributes["name"].InnerText + n3Url.Substring(n3Url.LastIndexOf('.')),
                                    (config) =>
                                    {
                                        // 下载成功代码
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{node3.Name}[{node3.Attributes["name"].InnerText}] : {n3Url}(Version:{node3.Attributes["version"].InnerText}) 下载成功");
                                    },
                                    (config) =>
                                    {
                                        // 文件存在代码
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{node3.Name}[{node3.Attributes["name"].InnerText}] : {n3Url}(Version:{node3.Attributes["version"].InnerText}) 已存在");
                                    },
                                    (config) =>
                                    {
                                        // 下载失败代码
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{node3.Name}[{node3.Attributes["name"].InnerText}] : {n3Url}(Version:{node3.Attributes["version"].InnerText}) 下载失败,但还是会继续下载。");
                                    },
                                    "PreLoadFilesList"
                                );
                            }
                        }
                    }

                    // 获取老鼠资源
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"正在获取老鼠资源...");
                    xDoc = new XmlDocument();
                    xDoc.Load("Sources/UnpackThe/FileList/config/ConfigFilePackage/mouse_desc.xml");
                    rootDec = "DownLoadFile/老鼠/";
                    foreach (XmlNode node1 in xDoc.DocumentElement.ChildNodes)
                    {
                        string n1Dec = rootDec + node1.Attributes["name"].InnerText + "/";
                        SourceFile_ThdDown.Push(
                                    node1.Attributes["id"].InnerText,
                                    $"https://q.ms.huanlecdn.com/4399/cdn.123u.com/resource/mouse/{node1.Attributes["id"].InnerText}.swf",
                                    n1Dec,
                                    node1.Attributes["id"].InnerText + ".swf",
                                    node1.Attributes["name"].InnerText,
                                    (config) =>
                                    {
                                        // 下载成功代码
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载成功");
                                    },
                                    (config) =>
                                    {
                                        // 文件存在代码
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 已存在");
                                    },
                                    (config) =>
                                    {
                                        // 下载失败,换一种下载方法
                                        SourceFile_ThdDown.DownloadInvoke(
                                            config,
                                            $"https://q.ms.huanlecdn.com/4399/cdn.123u.com/resource/{"0x80" + config.SourceID.Substring(config.FileName.IndexOf('x') + 1)}.swf",
                                            (config) =>
                                            {
                                                // 下载成功
                                                SourceFile_ThdDown.ErrorFileNum--;
                                                SourceFile_ThdDown.FileDoneNum++;
                                                Console.ForegroundColor = ConsoleColor.Green;
                                                Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载成功。");
                                            },
                                            (config) =>
                                            {
                                                // 下载失败
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载失败，但还是会继续下载。");
                                            }
                                       );
                                    },
                                    "老鼠"
                                );
                    }

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"获取地图资源...");
                    xDoc = new XmlDocument();
                    xDoc.Load("Sources/UnpackThe/FileList/config/ConfigFilePackage/map_desc.xml");
                    rootDec = "DownLoadFile/Map/";
                    foreach (XmlNode node1 in xDoc.DocumentElement.ChildNodes)
                    {
                        string n1Dec = rootDec + node1.Attributes["island_name"].InnerText + "/";
                        string fileName = "0xE0" + node1.Attributes["island_id"].InnerText.Substring(node1.Attributes["island_id"].InnerText.LastIndexOf('x') + 1).ToUpper().Replace("4000", "") + ".swf";
                        SourceFile_ThdDown.Push(
                                    fileName.Substring(0, fileName.LastIndexOf('.')),
                                    $"https://q.ms.huanlecdn.com/4399/cdn.123u.com/resource/{fileName}",
                                    n1Dec,
                                    fileName,
                                    node1.Attributes["island_name"].InnerText,
                                    (config) =>
                                    {
                                        // 下载成功代码
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载成功。");
                                    },
                                    (config) =>
                                    {
                                        // 文件存在代码
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 已存在");
                                    },
                                    (config) =>
                                    {
                                        // 下载失败代码
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载失败,但还是会继续下载。");
                                    },
                                    "地图"
                                );
                    }

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"获取道具资源...");
                    xDoc = new XmlDocument();
                    xDoc.Load("Sources/UnpackThe/FileList/config/ConfigFilePackage/card_desc.xml");
                    rootDec = "DownLoadFile/道具/";
                    foreach (XmlNode node1 in xDoc.DocumentElement.ChildNodes)
                    {
                        string n1Dec = (rootDec + node1.Attributes["type"].InnerText + "/" + node1.Attributes["name"].InnerText + "/").Replace('<', ' ').Replace('>', ' ').Replace('|', ' ').Replace('\"', ' ').Replace("\n", "").Replace('\n', ' ');
                        string fileName = node1.Attributes["id"].InnerText.ToUpper().Replace('X', 'x') + ".swf";
                        // 有些道具特殊,这个文件夹必须自己建
                        try
                        {
                            Directory.CreateDirectory(n1Dec);
                        }
                        catch (Exception)
                        {
                            n1Dec = rootDec + node1.Attributes["id"].InnerText + "/";
                            Directory.CreateDirectory(n1Dec);
                        }
                        // 数据写出
                        if (!new FileInfo(n1Dec + "数据.txt").Exists)
                        {
                            FileStream stream = new FileStream(n1Dec + "数据.txt", FileMode.Create);
                            string data = $"道具说明:{node1.Attributes["desc"].InnerText}\n" +
                                $"范围:{node1.Attributes["effect_area"].InnerText}\n" +
                                $"冷却时间:{node1.Attributes["energy"].InnerText}\n" +
                                $"强化后提升:{node1.Attributes["strengthen_desc"].InnerText}\n" +
                                $"类型:{node1.Attributes["type"].InnerText}\n" +
                                $"携带后增加体力:{node1.Attributes["hp"].InnerText}\n" +
                                $"性别:{node1.Attributes["sex"].InnerText}\n";
                            stream.Write(Encoding.UTF8.GetBytes(data), 0, Encoding.UTF8.GetBytes(data).Length);
                            stream.Flush();
                            stream.Close();
                        }
                        // 下载swf
                        SourceFile_ThdDown.Push(
                                    node1.Attributes["id"].InnerText.ToUpper().Replace('X', 'x'),
                                    $"https://q.ms.huanlecdn.com/4399/cdn.123u.com/resource/{fileName}",
                                    n1Dec,
                                    fileName,
                                    node1.Attributes["name"].InnerText,
                                    (config) =>
                                    {
                                        // 下载成功代码
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载成功。");
                                    },
                                    (config) =>
                                    {
                                        // 文件存在代码
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 已存在");
                                    },
                                    (config) =>
                                    {
                                        // 下载失败代码
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载失败,但还是会继续下载。");
                                    },
                                    "道具"
                                );
                        // 下载缩略图
                        string imgUrl = fileName[2] + "/" + fileName[3] + "/" + node1.Attributes["id"].InnerText + ".png";
                        SourceFile_ThdDown.Push(
                                    node1.Attributes["id"].InnerText.ToUpper().Replace('X', 'x') + "spic",
                                    $"https://q.ms.huanlecdn.com/4399/cdn.123u.com/images/{imgUrl}",
                                    n1Dec,
                                    "缩略图.png",
                                    node1.Attributes["name"].InnerText + "(缩略图)",
                                    (config) =>
                                    {
                                        // 下载成功代码
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载成功。");
                                    },
                                    (config) =>
                                    {
                                        // 文件存在代码
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 已存在");
                                    },
                                    (config) =>
                                    {
                                        // 下载失败代码
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载失败,但还是会继续下载。");
                                    },
                                    "道具缩略图"
                                );
                    }

                    Console.WriteLine($"获取其他资源...");
                    xDoc = new XmlDocument();
                    xDoc.Load("Sources/UnpackThe/FileList/config/ConfigFilePackage/versionMD5.xml");
                    rootDec = "DownLoadFile/Other/";
                    foreach (XmlNode node1 in xDoc.DocumentElement.ChildNodes) {
                        string fileName = node1.Attributes["path"].InnerText.Substring(node1.Attributes["path"].InnerText.LastIndexOf('/') + 1);
                        fileName = fileName.Substring(0, fileName.Length - 4);
                        string dec = rootDec + node1.Attributes["path"].InnerText.Substring(0, node1.Attributes["path"].InnerText.LastIndexOf('/') + 1);
                        SourceFile_ThdDown.Push(
                                    fileName,
                                    $"https://q.ms.huanlecdn.com/4399/cdn.123u.com/{node1.Attributes["path"].InnerText}",
                                    dec,
                                    fileName + ".swf",
                                    fileName,
                                    (config) =>
                                    {
                                        // 下载成功代码
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载成功。");
                                    },
                                    (config) =>
                                    {
                                        // 文件存在代码
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 已存在");
                                    },
                                    (config) =>
                                    {
                                        // 下载失败代码
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"({SourceFile_ThdDown.Percent}) > [{config.FileType}] :{config.FileName} 下载失败,但还是会继续下载。");
                                    },
                                    "道具缩略图"
                                );
                    }

                        Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"获取到资源{SourceFile_ThdDown.AllFileNum}个,两秒后开始下载...");

                    SourceFile_ThdDown.Start(() =>
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"所有资源下载成功,其中 成功{SourceFile_ThdDown.FileDoneNum}个,失败{SourceFile_ThdDown.ErrorFileNum}个,文件已存在{SourceFile_ThdDown.FileExistsNum}个");
                        Console.ReadKey();
                        return;
                    });
                });
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine("程序结束,按下任意键继续");
                Console.ReadKey();
            }
        }
    }
}
