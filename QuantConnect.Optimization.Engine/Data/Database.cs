using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Dapper.Contrib.Extensions;
using QuantConnect.Statistics;

namespace QuantConnect.Optimization.Engine.Data
{
    public class Database
    {
        public SQLiteConnection DB { get; private set; }
        
        public Database(string file)
        {
            DB = new SQLiteConnection(string.Format("URI=file:{0}", file));
            CreateSchema();
        }

        public int InsertPermutation(Dictionary<string, Tuple<Type, object>> permutation)
        {
            var desc = string.Join(",", permutation.Select(x => string.Format("{0}:{1}", x.Key, x.Value.Item2)));

            return (int)DB.Insert(new Test
            {
                Vars = desc
            });
        }

        public void InsertStatistics(StatisticsResults stats, int testId)
        {
            // save summary
            foreach (var item in stats.Summary)
            {
                DB.Insert(new Stat(testId, item.Key, item.Value));
            }

            // save trade statistics
            foreach (var pi in stats.TotalPerformance.TradeStatistics.GetType().GetProperties())
            {
                DB.Insert(new Stat(
                    testId,
                    string.Format("Trade.{0}", pi.Name),
                    pi.GetValue(stats.TotalPerformance.TradeStatistics).ToString()));
            }

            // save trade Portfolio Statistics
            foreach (var pi in stats.TotalPerformance.PortfolioStatistics.GetType().GetProperties())
            {
                DB.Insert(new Stat(
                    testId,
                    string.Format("Portfolio.{0}", pi.Name),
                    pi.GetValue(stats.TotalPerformance.PortfolioStatistics).ToString()));
            }

            // save trades
            foreach (var t in stats.TotalPerformance.ClosedTrades)
            {
                DB.Insert(new Trade
                {
                    TestId = testId,
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

        private void CreateSchema()
        {
            var sql = @"
                create table if not exists Test (
                    Id INTEGER PRIMARY KEY,
                    Vars text
                );

                create table if not exists Chart (
                    Id INTEGER PRIMARY KEY,
                    TestId int,
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
                    ChartId int,
                    Time int,
                    Value double
                );

                create table if not exists Stat (
                    TestId int,
                    Key text,
                    Value double,
                    Unit text
                );

                create table if not exists [Order] (
                    OrderId int,
                    TestId int,
                    Type int,
                    Time int,
                    Price double,
                    Symbol text,
                    Quantity int,
                    Status int,
                    Direction int
                );

                create table if not exists Trade (
                    TestId int,
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

            DB.Open();
            var cmd = DB.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
            DB.Close();
        }


    }

    [Table("Test")]
    public class Test
    {
        public int Id { get; set; }
        public string Vars { get; set; }
    }

    [Table("Chart")]
    public class Chart
    {
        public int Id { get; set; }
        public int TestId { get; set; }
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
        public Stat(int testId, string key, string strVal)
        {
            TestId = testId;
            Key = key;

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

        public int TestId { get; set; }
        public string Key { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
    }

    [Table("[Order]")]
    public class Order
    {
        public int OrderId { get; set; }
        public int TestId { get; set; }
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
        public int TestId { get; set; }
        public long EntryTime { get; set; }
        public decimal EntryPrice { get; set; }
        public long ExitTime { get; set; }
        public decimal ExitPrice { get; set; }
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public int Direction { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal TotalFees { get; set; }
        public decimal MAE { get; set; }
        public decimal MFE { get; set; }
        public double Duration { get; set; }
        public decimal EndTradeDrawdown { get; set; }
    }
}