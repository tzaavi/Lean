using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json;
using QuantConnect.Statistics;

namespace QuantConnect.Optimization.Engine.Data
{
    public class Database
    {
        private string _connectionString;

        public SQLiteConnection Connection()
        {
            return new SQLiteConnection(_connectionString); 
        }

        public Database(string file)
        {
            _connectionString = string.Format("URI=file:{0}", file);
            CreateSchema();
        }

        public int InsertPermutation(Dictionary<string, Tuple<Type, object>> permutation)
        {
            var conn = Connection();

            // add test
            var exId = (int)conn.Insert(new Execution
            {
                Desc = string.Join(",", permutation.Select(x => string.Format("{0}:{1}", x.Key, x.Value.Item2)))
            });

            // add parameters
            foreach (var paramater in permutation)
            {
                conn.Insert(new Parameter
                {
                    ExId = exId,
                    Name = paramater.Key,
                    Value =  Convert.ToDouble(paramater.Value.Item2)
                });
            }


            return exId;
        }

        public void InsertStatistics(StatisticsResults stats, int exId)
        {
            var conn = Connection();

            // save summary
            foreach (var item in stats.Summary)
            {
                conn.Insert(new Stat(exId, item.Key, item.Value, "Summary"));
            }

            // save trade statistics
            foreach (var pi in stats.TotalPerformance.TradeStatistics.GetType().GetProperties())
            {
                conn.Insert(new Stat(
                    exId,
                    pi.Name,
                    pi.GetValue(stats.TotalPerformance.TradeStatistics).ToString(),
                    "Trade"));
            }

            // save trade Portfolio Statistics
            foreach (var pi in stats.TotalPerformance.PortfolioStatistics.GetType().GetProperties())
            {
                conn.Insert(new Stat(
                    exId,
                    pi.Name,
                    pi.GetValue(stats.TotalPerformance.PortfolioStatistics).ToString(),
                    "Portfolio"));
            }

            // save trades
            foreach (var t in stats.TotalPerformance.ClosedTrades)
            {
                conn.Insert(new Trade
                {
                    ExId = exId,
                    Direction = (int) t.Direction,
                    Duration = t.Duration.TotalSeconds,
                    EndTradeDrawdown = t.EndTradeDrawdown,
                    EntryPrice = t.EntryPrice,
                    EntryTime = Convert.ToInt64(Time.DateTimeToUnixTimeStamp(t.EntryTime.ToUniversalTime())),
                    ExitPrice = t.ExitPrice,
                    ExitTime = Convert.ToInt64(Time.DateTimeToUnixTimeStamp(t.ExitTime.ToUniversalTime())),
                    MAE = t.MAE,
                    MFE = t.MFE,
                    ProfitLoss = t.ProfitLoss,
                    Quantity = t.Quantity,
                    Symbol = t.Symbol,
                    TotalFees = t.TotalFees
                });
            }
        }

        public void InsertCharts(IEnumerable<QuantConnect.Chart> charts, int exId)
        {
            var conn = Connection();

            foreach (var chart in charts)
            {
                var chartId = conn.Insert(new Chart
                {
                    ChartType = (int) chart.ChartType,
                    Name = chart.Name,
                    ExId = exId
                });

                foreach (var s in chart.Series)
                {
                    var seriesId = conn.Insert(new ChartSeries
                    {
                        ChartId = (int) chartId,
                        Name = s.Value.Name,
                        SeriesType = (int) s.Value.SeriesType,
                        Unit = s.Value.Unit
                    });


                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        foreach (var point in s.Value.Values)
                        {
                            conn.Insert(new ChartPoint
                            {
                                SeriesId = (int)seriesId,
                                Time = point.x,
                                Value = point.y
                            });
                        }
                        
                        trans.Commit();
                       conn.Close();
                    }
                    
              
                }
            }
        }

        private void CreateSchema()
        {
            var conn = Connection();

            var sql = @"
                create table if not exists Execution (
                    Id INTEGER PRIMARY KEY,
                    Desc text
                );

                create table if not exists Parameter (
                    ExId int,
                    Name text,
                    Value double
                );

                create table if not exists Chart (
                    Id INTEGER PRIMARY KEY,
                    ExId int,
                    Name text,
                    ChartType int
                );

                create table if not exists ChartSeries (
                    Id INTEGER PRIMARY KEY,
                    ChartId int,
                    Name text,
                    Unit text,
                    SeriesType int
                );

                create table if not exists ChartPoint (
                    SeriesId int,
                    Time int,
                    Value double
                );

                create table if not exists Stat (
                    ExId int,
                    Key text,
                    Name text,
                    Category text,
                    Value double,
                    Unit text
                );

                create table if not exists [Order] (
                    OrderId int,
                    ExId int,
                    Type int,
                    Time int,
                    Price double,
                    Symbol text,
                    Quantity int,
                    Status int,
                    Direction int
                );

                create table if not exists Trade (
                    ExId int,
                    EntryTime int,
                    EntryPrice double,
                    ExitTime int,
                    ExitPrice double,
                    Symbol text,
                    Quantity int,
                    Direction int,
                    ProfitLoss double,
                    TotalFees double,
                    MAE double,
                    MFE double,
                    Duration int,
                    EndTradeDrawdown double
                );
            ";

            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
            conn.Close();
        }


    }

    [Table("Execution")]
    public class Execution
    {
        public int Id { get; set; }
        public string Desc { get; set; }
    }

    [Table("Parameter")]
    public class Parameter
    {
        public int ExId { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
    }

    [Table("Chart")]
    public class Chart
    {
        public int Id { get; set; }
        public int ExId { get; set; }
        public string Name { get; set; }
        public int ChartType { get; set; }
    }

    [Table("ChartSeries")]
    public class ChartSeries
    {
        public int Id { get; set; }
        public int ChartId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public int SeriesType { get; set; }
    }

    [Table("ChartPoint")]
    public class ChartPoint
    {
        public int SeriesId { get; set; }
        public long Time { get; set; }
        public decimal Value { get; set; }
    }

    [Table("Stat")]
    public class Stat
    {
        public Stat() { }

        public Stat(int exId, string name, string strVal, string category)
        {
            ExId = exId;
            Name = name;
            Category = category;

            // gen key from name category
            Key = string.Format("{0}-{1}", category, name).ToLower().Replace(" ", "-").Trim();

            // check for % unit
            if (strVal.Contains("%"))
            {
                Unit = "%";
                strVal = strVal.Replace("%", "");
            }

            // check for $ unit
            if (strVal.Contains("$"))
            {
                Unit = "$";
                strVal = strVal.Replace("$", "");
            }

            // try to parse number
            double val;
            if (double.TryParse(strVal, out val))
            {
                Value = val;
                return;
            }

            // try to parse duration
            TimeSpan duration;
            if (TimeSpan.TryParse(strVal, out duration))
            {
                Value = duration.TotalSeconds;
                return;
            }

            // try to parse time
            DateTime time;
            if (DateTime.TryParse(strVal, out time))
            {
                Value = Time.DateTimeToUnixTimeStamp(time.ToUniversalTime());
            }
        }

        public int ExId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
    }

    [Table("[Order]")]
    public class Order
    {
        public int OrderId { get; set; }
        public int ExId { get; set; }
        public int Type { get; set; }
        public long Time { get; set; }
        public decimal Price { get; set; }
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public int Status { get; set; }
        public int Direction { get; set; }
    }

    [Table("Trade")]
    public class Trade
    {
        public int ExId { get; set; }
        public long EntryTime { get; set; }
        public decimal EntryPrice { get; set; }
        public long ExitTime { get; set; }
        public decimal ExitPrice { get; set; }
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public int Direction { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal TotalFees { get; set; }
        [JsonProperty("mae")]
        public decimal MAE { get; set; }
        [JsonProperty("mfe")]
        public decimal MFE { get; set; }
        public double Duration { get; set; }
        public decimal EndTradeDrawdown { get; set; }
    }
}