using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml;

namespace GetAllSource
{


    class Program
    {
        static void Main(string[] args)
        {
            List<string> idList = new List<string>();
            /*
                errorFileNum    错误的文件数
                fileDoneNum     完成的文件数
                allFileNum      文件总数
             */
            int errorFileNum = 0, fileDoneNum = 0, fileExists = 0, allFileNum = 0;
            // 获取LoadFilesList.xml(这个文件是直接用的美食官网的)
            Stream st = WebRequest.Create("https://q.ms.huanlecdn.com/4399/cdn.123u.com/LoadFilesList.xml").GetResponse().GetResponseStream();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(new StreamReader(st).ReadToEnd());
            string path = "Source/";
            foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
            {
                string baseurl = node.Attributes["baseurl"].InnerText;
                Directory.CreateDirectory(path + "FileList/" + baseurl);
                foreach (XmlNode _node in node.ChildNodes)
                {
                    allFileNum++;
                    if (new FileInfo(path + "FileList/" + baseurl + _node.Attributes["url"].InnerText).Exists)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"{_node.Attributes["url"].InnerText} 已存在");
                        fileExists++;
                        continue;
                    }
                    if (!DownloadClass.DownloadFile($"https://q.ms.huanlecdn.com/4399/cdn.123u.com/{baseurl}{_node.Attributes["url"].InnerText}",
                        path + "FileList/" + baseurl + _node.Attributes["url"].InnerText, null))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{_node.Attributes["url"].InnerText} 下载失败 但是程序还会继续运行 去提取下一个文件");
                        errorFileNum++;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{_node.Attributes["url"].InnerText} 下载完成");
                        fileDoneNum++;
                        string fileName = _node.Attributes["url"].InnerText.Split('.')[1];
                        if (fileName == "dat" || fileName == "txt")
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine($"{_node.Attributes["url"].InnerText} 开始解压");
                            Directory.CreateDirectory(path + "UnpackThe/" + baseurl + _node.Attributes["url"].InnerText.Split('.')[0]);
                            ZipFile.ExtractToDirectory(path + "FileList/" + baseurl + _node.Attributes["url"].InnerText,
                                path + "UnpackThe/" + baseurl + _node.Attributes["url"].InnerText.Split('.')[0]);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"{_node.Attributes["url"].InnerText} 解压完成");
                        }
                    }
                }
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"初始文件全部下载完毕,开始下载资源文件...");
            // 搭建框架
            /*
                PreLoadFilesList.xml
                位置: Source\FileList\PreLoadFilesList.xml
             */
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("Source/FileList/PreLoadFilesList.xml");
            XmlElement root = xDoc.DocumentElement;
            string rootDec = path + "DownLoadFile/PreLoadFilesList/";
            foreach (XmlNode node1 in root.ChildNodes)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"当前节点:  {node1.Name}\n");
                string baseUrl = node1.Attributes["baseurl"].InnerText;
                foreach (XmlNode node2 in node1.ChildNodes)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"   |- 类型:{node2.Name}");
                    Directory.CreateDirectory(rootDec + node2.Name);
                    foreach (XmlNode node3 in node2.ChildNodes)
                    {
                        allFileNum++;
                        string n3Url = node3.Attributes["url"].InnerText;
                        string n3Dec = rootDec + node2.Name + "/" + baseUrl + (n3Url.LastIndexOf('/') == -1 ? "" : n3Url.Substring(0, n3Url.LastIndexOf('/') + 1));
                        Directory.CreateDirectory(n3Dec);
                        idList.Add(node3.Attributes["name"].InnerText);
                        if (new FileInfo(n3Dec + node3.Attributes["name"].InnerText + n3Url.Substring(n3Url.LastIndexOf('.'))).Exists)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"     |- {node3.Name}[{node3.Attributes["name"].InnerText}] : {n3Url}(Version:{node3.Attributes["version"].InnerText}) 已存在");
                            fileExists++;
                            continue;
                        }
                        if (!DownloadClass.DownloadFile($"https://q.ms.huanlecdn.com/4399/cdn.123u.com/{baseUrl}{n3Url}",
                            n3Dec + node3.Attributes["name"].InnerText + n3Url.Substring(n3Url.LastIndexOf('.')), null))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"     |- {node3.Name}[{node3.Attributes["name"].InnerText}] : {n3Url}(Version:{node3.Attributes["version"].InnerText}) 下载失败,但还是会继续下载。");
                            errorFileNum++;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"     |- {node3.Name}[{node3.Attributes["name"].InnerText}] : {n3Url}(Version:{node3.Attributes["version"].InnerText}) 下载成功");
                            fileDoneNum++;
                        }
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"下载老鼠资源...");
            xDoc = new XmlDocument();
            xDoc.Load("Source/UnpackThe/config/ConfigFilePackage/mouse_desc.xml");
            root = xDoc.DocumentElement;
            rootDec = path + "DownLoadFile/老鼠/";
            int index = 0;
            foreach (XmlNode node1 in root.ChildNodes)
            {
                allFileNum++;
                string n1Dec = rootDec + node1.Attributes["name"].InnerText + "/";
                Directory.CreateDirectory(n1Dec);
                idList.Add(node1.Attributes["id"].InnerText);
                index++;
                if (new FileInfo(n1Dec + node1.Attributes["id"].InnerText + ".swf").Exists)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{node1.Attributes["name"].InnerText} 已存在");
                    fileExists++;
                    continue;
                }
                if (!DownloadClass.DownloadFile($"https://q.ms.huanlecdn.com/4399/cdn.123u.com/resource/mouse/{node1.Attributes["id"].InnerText + ".swf"}",
                    n1Dec + node1.Attributes["id"].InnerText + ".swf", null))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{node1.Attributes["name"].InnerText} 下载失败，尝试换一种方式下载,但还是会继续下载。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                    if (!DownloadClass.DownloadFile($"https://q.ms.huanlecdn.com/4399/cdn.123u.com/resource/{"0x80" + node1.Attributes["id"].InnerText.Substring(node1.Attributes["id"].InnerText.IndexOf('x') + 1) + ".swf"}",
                    n1Dec + node1.Attributes["id"].InnerText + ".swf", null))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{node1.Attributes["name"].InnerText} 下载失败，但还是会继续下载。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                        errorFileNum++;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{node1.Attributes["name"].InnerText} 下载成功。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                        fileDoneNum++;
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{node1.Attributes["name"].InnerText} 下载成功。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                    fileDoneNum++;
                }
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"下载地图资源...");
            xDoc = new XmlDocument();
            xDoc.Load("Source/UnpackThe/config/ConfigFilePackage/map_desc.xml");
            root = xDoc.DocumentElement;
            rootDec = path + "DownLoadFile/Map/";
            index = 0;
            foreach (XmlNode node1 in root.ChildNodes)
            {
                allFileNum++;
                string n1Dec = rootDec + node1.Attributes["island_name"].InnerText + "/";
                string fileName = "0xE0" + node1.Attributes["island_id"].InnerText.Substring(node1.Attributes["island_id"].InnerText.LastIndexOf('x') + 1).ToUpper().Replace("4000", "") + ".swf";
                Directory.CreateDirectory(n1Dec);
                idList.Add(fileName.Substring(0, fileName.LastIndexOf('.')));
                index++;
                if (new FileInfo(n1Dec + fileName).Exists)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{node1.Attributes["island_name"].InnerText} 已存在");
                    fileExists++;
                    continue;
                }
                if (!DownloadClass.DownloadFile($"https://q.ms.huanlecdn.com/4399/cdn.123u.com/resource/{fileName}",
                    n1Dec + fileName, null))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{fileName} 下载失败,但还是会继续下载。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                    errorFileNum++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{node1.Attributes["island_name"].InnerText} 下载成功。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                    fileDoneNum++;
                }
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"下载道具资源...");
            xDoc = new XmlDocument();
            xDoc.Load("Source/UnpackThe/config/ConfigFilePackage/card_desc.xml");
            root = xDoc.DocumentElement;
            rootDec = path + "DownLoadFile/道具/";
            index = 0;
            foreach (XmlNode node1 in root.ChildNodes)
            {
                allFileNum++;
                string n1Dec = (rootDec + node1.Attributes["type"].InnerText + "/" + node1.Attributes["name"].InnerText + "/").Replace('<', ' ').Replace('>', ' ').Replace('|', ' ').Replace('\"', ' ').Replace("\n", "").Replace('\n', ' ');
                string fileName = node1.Attributes["id"].InnerText.ToUpper().Replace('X', 'x') + ".swf";
                try
                {
                    Directory.CreateDirectory(n1Dec);
                }
                catch (Exception)
                {
                    n1Dec = rootDec + node1.Attributes["id"].InnerText + "/";
                    Directory.CreateDirectory(n1Dec);

                }
                idList.Add(node1.Attributes["id"].InnerText.ToUpper().Replace('X', 'x'));
                index++;
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
                // swf下载
                if (new FileInfo(n1Dec + fileName).Exists)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{node1.Attributes["name"].InnerText} 的swf已存在");
                }
                else if (node1.Attributes["effect_condition"].InnerText != string.Empty)
                {
                    if (!DownloadClass.DownloadFile($"https://q.ms.huanlecdn.com/4399/cdn.123u.com/resource/{fileName}",
                    n1Dec + fileName, null))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{$"https://q.ms.huanlecdn.com/4399/cdn.123u.com/resource/{fileName}"} SWF下载失败,但还是会继续下载。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{node1.Attributes["name"].InnerText} SWF下载成功。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                    }
                }

                // 道具图片下载
                if (new FileInfo(n1Dec + "缩略图.png").Exists)
                {
                    fileExists++;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{node1.Attributes["name"].InnerText} 的道具图片已存在");
                }
                else
                {
                    string imgUrl = fileName[2] + "/" + fileName[3] + "/" + node1.Attributes["id"].InnerText + ".png";
                    if (!DownloadClass.DownloadFile($"https://q.ms.huanlecdn.com/4399/cdn.123u.com/images/{imgUrl}",
                    n1Dec + "缩略图.png", null))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{$"https://q.ms.huanlecdn.com/4399/cdn.123u.com/images/{imgUrl}"} 缩略图下载失败,但还是会继续下载。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                        errorFileNum++;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{node1.Attributes["name"].InnerText} 缩略图下载成功。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                        fileDoneNum++;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"检测其他资源...");
            xDoc = new XmlDocument();
            xDoc.Load("Source/UnpackThe/config/ConfigFilePackage/versionMD5.xml");
            root = xDoc.DocumentElement;
            rootDec = path + "DownLoadFile/Other/";
            index = 0;
            foreach (XmlNode node1 in root.ChildNodes)
            {

                string fileName = node1.Attributes["path"].InnerText.Substring(node1.Attributes["path"].InnerText.LastIndexOf('/') + 1);
                fileName = fileName.Substring(0, fileName.Length - 4);
                string dec = rootDec + node1.Attributes["path"].InnerText.Substring(0, node1.Attributes["path"].InnerText.LastIndexOf('/') + 1);
                Directory.CreateDirectory(dec);
                index++;
                if (new FileInfo(dec + fileName + ".swf").Exists || idList.Contains(fileName))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{fileName} 已存在");
                    continue;
                }
                allFileNum++;
                if (!DownloadClass.DownloadFile($"https://q.ms.huanlecdn.com/4399/cdn.123u.com/{node1.Attributes["path"].InnerText}",
                    dec + fileName + ".swf", null))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{fileName} 下载失败,但还是会继续下载。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                    errorFileNum++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{fileName} 下载成功。({Math.Round(index * 1.0 / root.ChildNodes.Count * 100, 2)}%)");
                    fileDoneNum++;
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"提取完成，一共需要下载{allFileNum}个文件 成功下载了{fileDoneNum}个文件 失败了{errorFileNum}个文件 已存在{fileExists}个文件");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
