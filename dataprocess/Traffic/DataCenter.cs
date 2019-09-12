using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class DataCenterForTraffic
{

    public const string DataFolder = @"F:\CCF-Visualization\RawData\海口市-交通流量时空演变特征可视分析";
    public const string EDAFile = @"F:\CCF-Visualization\dataprocess\AfterProcess\海口市-交通流量时空演变特征可视分析\EDA.log";
    public const string AfterProcessFolder = @"F:\CCF-Visualization\dataprocess\AfterProcess\海口市-交通流量时空演变特征可视分析\";

    public static List<OrderDetails> orders = new List<OrderDetails>();

    public static List<DiaryProperty> diarys = new List<DiaryProperty>();


    /// <summary>
    /// 加载数据
    /// </summary>
    public static void Load(int MaxRecord = -1)
    {
        var cnt = 0;
        foreach (var filename in Directory.GetFiles(DataFolder))
        {
            var sr = new StreamReader(filename);
            sr.ReadLine();  //Skip Title
            while (!sr.EndOfStream)
            {
                orders.Add(new OrderDetails(sr.ReadLine()));
                cnt++;
                if (cnt == MaxRecord) break;    //内存限制
            }
            sr.Close();
            if (cnt == MaxRecord) break;        //内存限制
        }
        Console.WriteLine("Total Record Count:" + orders.Count);
    }

    /// <summary>
    /// EDA
    /// </summary>
    public static void EDA()
    {
        //基本信息CSV
        var basic_sw_csv = new StreamWriter(AfterProcessFolder + "basic_info.csv");

        var TotalOrderCnt = orders.Count;
        var TotalFee = (int)orders.Sum(x => x.pre_total_fee);
        var TotalDistanceKm = (int)orders.Sum(x => x.start_dest_distance_km);

        basic_sw_csv.WriteLine("TotalOrderCnt," + TotalOrderCnt);
        basic_sw_csv.WriteLine("TotalFee," + TotalFee);
        basic_sw_csv.WriteLine("AvgFeePerOrder," + Math.Round((double)TotalFee / TotalOrderCnt, 2));
        basic_sw_csv.WriteLine("TotalDistanceKm," + TotalDistanceKm);
        basic_sw_csv.WriteLine("AvgDistanceKmPerOrder," + Math.Round((double)TotalDistanceKm / TotalOrderCnt, 4));
        basic_sw_csv.WriteLine("FeePerKm," + Math.Round((double)TotalFee / TotalDistanceKm, 2));
        basic_sw_csv.Flush();

        //1-1.订单量 按照日期统计 
        //部分订单时间为 0000-00-00 这里按照最后的日期为依据
        var sw_csv = new StreamWriter(AfterProcessFolder + "diary_orderCnt.csv");
        var diaryinfos = orders.GroupBy(x => x.year.ToString("D4") + x.month.ToString("D2") + x.day.ToString("D2"))
                          .Select(x => (name: x.Key, count: x.Count(), distance: x.ToList().Sum(o => o.start_dest_distance_km), fee: x.ToList().Sum(o => o.pre_total_fee))).ToList();
        diaryinfos.Sort((x, y) => { return x.name.CompareTo(y.name); });
        foreach (var item in diaryinfos)
        {
            sw_csv.WriteLine(item.name + "," + item.count + "," + Math.Round(item.distance) + "," + Math.Round(item.fee));
        }
        sw_csv.Close();

        var TotalDayCnt = diaryinfos.Count;

        basic_sw_csv.WriteLine("TotalDayCnt," + TotalDayCnt);
        basic_sw_csv.WriteLine("AvgOrderCntEveryDay," + TotalOrderCnt / TotalDayCnt);
        basic_sw_csv.WriteLine("AvgFeeEveryDay," + TotalFee / TotalDayCnt);
        basic_sw_csv.WriteLine("AvgDistanceKmEveryDay," + TotalDistanceKm / TotalDayCnt);
        basic_sw_csv.Flush();

        //2-1:对于时间段进行统计
        var weekday_hour_orderCnt = orders.GroupBy(x => x.departure_time.DayOfWeek.GetHashCode() + "|" +
                                                   x.departure_time.Hour.ToString("D2") + ":" + ((x.departure_time.Minute / 15) * 15).ToString("D2"))
                                          .Select(x => (name: x.Key, count: x.Count())).ToList();
        weekday_hour_orderCnt.Sort((x, y) => { return x.name.CompareTo(y.name); });
        sw_csv = new StreamWriter(AfterProcessFolder + "weekday_hour_orderCnt.csv");
        foreach (var item in weekday_hour_orderCnt)
        {
            sw_csv.WriteLine(item.name + "," + item.count);
        }
        sw_csv.Close();


        var diary_HourCnt = orders.GroupBy(x => x.departure_time.Date)
                                  .Select(x => (name: x.Key, count: x.Count())).ToList();
        diary_HourCnt.Sort((x, y) => { return x.name.CompareTo(y.name); });

        //3-1：出发和目的分析
        var startlocs = orders.GroupBy(x => x.starting).Select(x => (point: x.Key, count: x.Count())).ToList();
        CreateGeoJson("startlocs", startlocs);
        var destlocs = orders.GroupBy(x => x.dest).Select(x => (point: x.Key, count: x.Count())).ToList();
        CreateGeoJson("destlocs", destlocs);

        //3-2 深夜打车地点的统计

/*         startlocs.Sort((x, y) =>
        {
            if (x.point.lat == y.point.lat)
            {
                return x.point.lng.CompareTo(y.point.lng);
            }
            else
            {
                return x.point.lat.CompareTo(y.point.lat);
            }
        }); */

        var sw = new StreamWriter(EDAFile);
        sw.WriteLine(DiaryProperty.GetTitle());
        foreach (var diary in diarys)
        {
            sw.WriteLine(diary.ToString());
        }

        sw.WriteLine("每个时间点的订单数:");
        foreach (var item in diary_HourCnt)
        {
            sw.WriteLine(item.name + ":" + item.count);
        }
        sw.WriteLine("Start Loc Count:" + startlocs.Count);
        sw.WriteLine("Dest  Loc Count:" + destlocs.Count);


        //8.对于里程数的统计
        basic_sw_csv.Write("Distance,");
        basic_sw_csv.Write("小于5公里," + orders.Count(x => x.start_dest_distance_km <= 5) + ",");
        basic_sw_csv.Write("5-10公里," + orders.Count(x => x.start_dest_distance_km > 5 && x.start_dest_distance_km <= 10) + ",");
        basic_sw_csv.Write("10-20公里," + orders.Count(x => x.start_dest_distance_km > 10 && x.start_dest_distance_km <= 20) + ",");
        basic_sw_csv.Write("大于20公里," + orders.Count(x => x.start_dest_distance_km > 20) + ",");
        basic_sw_csv.WriteLine();

        basic_sw_csv.Write("Time,");
        basic_sw_csv.Write("小于15分钟," + orders.Count(x => x.normal_time != 0 && x.normal_time <= 15) + ",");
        basic_sw_csv.Write("15-30分钟," + orders.Count(x => x.normal_time > 15 && x.normal_time <= 30) + ",");
        basic_sw_csv.Write("30-60分钟," + orders.Count(x => x.normal_time > 30 && x.normal_time <= 60) + ",");
        basic_sw_csv.Write("大于60分钟," + orders.Count(x => x.normal_time > 60) + ",");
        basic_sw_csv.WriteLine();

        //9 各种区分统计
        //9-0 产品线ID
        var product_ids = orders.GroupBy(x => x.product_id).Select(x => (name: x.Key, count: x.Count())).ToList();
        sw.WriteLine("产品线ID[product_ids]:");
        basic_sw_csv.Write("product_ids,");
        foreach (var item in product_ids)
        {
            sw.WriteLine(item.name + ":" + item.count);
            basic_sw_csv.Write(item.name + "," + item.count + ",");
        }
        basic_sw_csv.WriteLine();

        //9-1 订单时效
        var order_types = orders.GroupBy(x => x.order_type).Select(x => (name: x.Key, count: x.Count())).ToList();
        sw.WriteLine("订单时效[order_type]:");
        basic_sw_csv.Write("order_type,");
        foreach (var item in order_types)
        {
            sw.WriteLine(item.name + ":" + item.count);
            basic_sw_csv.Write(item.name + "," + item.count + ",");
        }
        basic_sw_csv.WriteLine();

        //9-2 订单类型
        var combo_types = orders.GroupBy(x => x.combo_type).Select(x => (name: x.Key, count: x.Count())).ToList();
        sw.WriteLine("订单类型[combo_type]:");
        basic_sw_csv.Write("combo_type,");
        foreach (var item in combo_types)
        {
            sw.WriteLine(item.name + ":" + item.count);
            basic_sw_csv.Write(item.name + "," + item.count + ",");
        }
        basic_sw_csv.WriteLine();


        //9-3 交通类型
        var traffic_types = orders.GroupBy(x => x.traffic_type).Select(x => (name: x.Key, count: x.Count())).ToList();
        sw.WriteLine("交通类型[traffic_types]:");
        basic_sw_csv.Write("traffic_types,");
        foreach (var item in traffic_types)
        {
            sw.WriteLine(item.name + ":" + item.count);
            basic_sw_csv.Write(item.name + "," + item.count + ",");
        }
        basic_sw_csv.WriteLine();


        //9-4 一级业务线
        var product_1levels = orders.GroupBy(x => x.product_1level).Select(x => (name: x.Key, count: x.Count())).ToList();
        sw.WriteLine("一级业务线[product_1levels]:");
        basic_sw_csv.Write("product_1levels,");
        foreach (var item in product_1levels)
        {
            sw.WriteLine(item.name + ":" + item.count);
            basic_sw_csv.Write(item.name + "," + item.count + ",");
        }
        basic_sw_csv.WriteLine();


        basic_sw_csv.Close();
        sw.Close();
        orders.Clear();
        GC.Collect();
    }




    private static void CreateGeoJson(string filename, List<(OrderDetails.Geo point, System.Int32 count)> points)
    {
        const double baiduOffsetlng = 0.0063;
        const double baiduOffsetlat = 0.0058;
        var json = new StreamWriter(AfterProcessFolder + filename + "_PointSize.json");
        int Cnt = 0;
        json.WriteLine("[");
        foreach (var item in points)
        {
            var radus = item.count;
            if (radus > 1000)
            {
                if (Cnt != 0) json.WriteLine(",");
                Cnt++;
                json.Write(" {\"name\": \"海口" + Cnt + "\", \"value\": ");
                json.Write("[" + Math.Round(item.point.lng + baiduOffsetlng, 4)
                                                + "," + Math.Round(item.point.lat + baiduOffsetlat, 4) + "," + radus + "]}");
            }
        }
        json.WriteLine();
        json.WriteLine("]");
        json.Close();
    }
}

/// <summary>
/// 每日属性
/// </summary>
public class DiaryProperty
{
    public string Date { get; set; }

    /// <summary>
    /// 订单总量
    /// </summary>
    /// /// <value></value>
    public int OrderCnt { get; set; }
    /// <summary>
    /// 营业额总量
    /// </summary>
    /// <value></value>
    public Single TotalFee { get; set; }

    /// <summary>
    /// 平均费用
    /// </summary>
    /// <value></value>
    public Single AvgFee
    {
        get
        {
            return TotalFee / OrderCnt;
        }
    }
    public static string GetTitle()
    {
        return "日期,订单数,总费用,每单费用";
    }
    public override String ToString()
    {
        return Date + "," + OrderCnt + "," + TotalFee + "," + AvgFee;
    }

}
