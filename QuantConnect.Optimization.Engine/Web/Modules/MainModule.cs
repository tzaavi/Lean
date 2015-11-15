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

            Get["/optimization"] = parameters =>
            {
                if(_db == null)
                    return Response.AsRedirect("/source/select");

                return View["optimization"];
            };

            
            Get["/api/charts"] = parameters =>
            {
                return new[]
                {
                    new Data.ChartPoint {SeriesId = 1},
                    new Data.ChartPoint {SeriesId = 2}
                };
            };

            Get["/api/tests"] = parameters =>
            {
                var conn = _db.Connection();
                return conn.Query<Test>("select * from Test");
            };

            Get["/api/optimization/parameters"] = parameters =>
            {
                var conn = _db.Connection();
                return conn.Query("select distinct(Name) from Parameter");
            };

            Get["/api/optimization/dimentions"] = parameters =>
            {
                var conn = _db.Connection();
                return conn.Query("select distinct(Name) from Stat");
            };

            Get["/api/optimization/stats"] = parameters =>
            {
                var conn = _db.Connection();

                var valueList = conn.Query<Stat>("select * from Stat").ToList();
                var paramList = conn.Query<Parameter>("select * from Parameter").ToList();

                return paramList.GroupBy(x => x.TestId).Select(x =>
                {
                    dynamic test = new ExpandoObject();
                    test.TestId = x.Key;
                    test.Parameters = x.OrderBy(p => p.Name).ToDictionary(p => p.Name, p => p.Value);
                    test.Values = valueList.OrderBy(v => v.Name).Where(v => v.TestId == x.Key).ToDictionary(v => v.Name, v => v.Value);
                    return test;
                });
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