using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using Dapper;
using Nancy;
using QuantConnect.Configuration;
using QuantConnect.Optimization.Engine.Data;

namespace QuantConnect.Optimization.Engine.Web.Modules
{
    public class MainModule : NancyModule
    {
        private static Database _db;

        public MainModule()
        {
            Get["/source/select"] = parameters =>
            {
                return View["source", new {Files = GetSourceFiles()}];
            };

            Get["/source/{file}"] = parameters =>
            {
                InitDatabase(parameters.file);

                return Response.AsRedirect("/optimization");
            };

            Get["/source/{file}/{exId}"] = parameters =>
            {
                InitDatabase(parameters.file);

                string path = "/executions/" + parameters.exId;

                return Response.AsRedirect(path);
            };

            Get["/optimization"] = parameters =>
            {
                if(_db == null)
                    return Response.AsRedirect("/source/select");

                return View["optimization"];
            };

            Get["/executions/{id}"] = parameters =>
            {
                return View["Execution", new {ExId = parameters.id}];
            };

            Get["/api/optimization/stats"] = parameters =>
            {
                var conn = _db.Connection();
                var statList = conn.Query<Stat>("select * from Stat").ToList();
                var paramList = conn.Query<Parameter>("select * from Parameter").ToList();

                dynamic data = new ExpandoObject();
                data.Parameters = paramList.GroupBy(p => p.Name).Select(p => p.Key);
                data.DimDict = statList.GroupBy(s => s.Key).ToDictionary(g => g.Key, g => new{g.First().Name, g.First().Category});

                data.Stats = paramList.GroupBy(x => x.ExId).Select(x =>
                {
                    dynamic ex = new ExpandoObject();
                    ex.ExId = x.Key;
                    ex.Parameters = x.OrderBy(p => p.Name).ToDictionary(p => p.Name, p => p.Value);
                    ex.Values = statList.OrderBy(s => s.Key).Where(s => s.ExId == x.Key).ToDictionary(s => s.Key, s => s.Value);
                    return ex;
                });

                return data;
            };

            Get["/api/executions/{id}/stats"] = parameters =>
            {
                var conn = _db.Connection();
                return conn.Query<Stat>("select * from Stat where ExId = @ExId", new {ExId = parameters.id}).ToList();
            };

            Get["/api/executions/{id}/trades"] = parameters =>
            {
                var conn = _db.Connection();
                return conn.Query<Trade>("select * from Trade where ExId = @ExId", new { ExId = parameters.id }).ToList();
            };

            Get["/api/executions/{id}/charts"] = parameters =>
            {
                var conn = _db.Connection();
                return conn.Query<Data.Chart>("select * from Chart where ExId = @ExId", new { ExId = parameters.id }).ToList();
            };

            Get["/api/executions/{exId}/charts/{chartId}"] = parameters =>
            {
                var conn = _db.Connection();
                var chart = conn.Query<Data.Chart>("select * from Chart where Id = @Id", new { Id = parameters.chartId }).First();
                var series = conn.Query<ChartSeries>("select * from ChartSeries where ChartId = @ChartId", new {ChartId = parameters.chartId}).ToList();
            
                dynamic ret = new ExpandoObject();
                ret.chartType = chart.ChartType;
                ret.name = chart.Name;
                ret.id = chart.Id;

                ret.series = series.Select(x => new
                {
                    name = x.Name,
                    seriesType = x.SeriesType,
                    points = conn.Query<Data.ChartPoint>("select * from ChartPoint where SeriesId = @SeriesId", new {SeriesId = x.Id}).ToList()
                });

                return ret;
            };
        }

        private List<dynamic> GetSourceFiles()
        {
            var folder = Path.Combine(Config.Get("data-folder"), "optimization");

            var files = new List<dynamic>();

            foreach (var file in Directory.GetFiles(folder, "*.sqlite"))
            {
                var fileName = Path.GetFileName(file);
                var parts = fileName.Replace(".sqlite", "").Split('-');
                dynamic obj = new ExpandoObject();
                obj.File = fileName;
                obj.Algo = parts[0];
                obj.Time = DateTime.ParseExact(parts[1], "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                files.Add(obj);
            }

            return files.OrderByDescending(x => x.Time).ToList();
        }

        private void InitDatabase(string file)
        {
            var opzDataFolder = Path.Combine(Config.Get("data-folder"), "optimization");
            var dbFile = Path.Combine(opzDataFolder, file);
            _db = new Database(dbFile);
        }
    }
}