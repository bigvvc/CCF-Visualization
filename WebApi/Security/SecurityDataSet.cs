using System.Collections.Generic;
using System.IO;

public static class SecurityDataSet
{
    public static List<NameValueSet<int>> Protocols = new List<NameValueSet<int>>();
    public static List<NameValueSet<int>> Hours = new List<NameValueSet<int>>();
    public static List<NameValueSet<int>> Protocols_Hours = new List<NameValueSet<int>>();
    public static List<NameValueSet<long>> BasicInfo = new List<NameValueSet<long>>();


    public static void LoadData()
    {
        //协议统计的加载
        var Folder = @"F:\CCF-Visualization\dataprocess\AfterProcess\企业网络资产及安全事件分析与可视化\";
        //var Folder = @"/root/HelloChinaApi/AfterProcess/企业网络资产及安全事件分析与可视化/";

        var sr = new StreamReader(Folder + "protocols.csv");
        while (!sr.EndOfStream)
        {
            var info = sr.ReadLine().Split(",");
            Protocols.Add(new NameValueSet<int>() { Name = info[0], Value = int.Parse(info[1]) });
        }
        sr.Close();

        sr = new StreamReader(Folder + "hours.csv");
        while (!sr.EndOfStream)
        {
            var info = sr.ReadLine().Split(",");
            Hours.Add(new NameValueSet<int>() { Name = info[0], Value = int.Parse(info[1]) });
        }
        sr.Close();

        sr = new StreamReader(Folder + "protocols_hours.csv");
        while (!sr.EndOfStream)
        {
            var info = sr.ReadLine().Split(",");
            Protocols_Hours.Add(new NameValueSet<int>() { Name = info[0] + "|" + info[1], Value = int.Parse(info[2]) });
        }
        sr.Close();

        sr = new StreamReader(Folder + "basic_info.csv");
        while (!sr.EndOfStream)
        {
            var info = sr.ReadLine().Split(",");
            if (info[0] == nameof(DashBoard.downlink_length) || info[0] == nameof(DashBoard.uplink_length))
            {
                BasicInfo.Add(new NameValueSet<long>() { Name = info[0], Value = (long)double.Parse(info[1]) });
            }
            else
            {
                BasicInfo.Add(new NameValueSet<long>() { Name = info[0], Value = long.Parse(info[1]) });
            }
        }
        sr.Close();

    }
}

