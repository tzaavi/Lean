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
                return conn.Query("select distinct(Key) from Stat");
            };

            Get["/api/ptimization/chart"] = parameters =>
            {
                var dim = Request.Query["dim"];
                var param1 = Request.Query["param1"];
                var param2 = Request.Query["param2"];

                var conn = _db.Connection();

                var valueList = conn.Query<Stat>("select * from Stat where Key = @Key", new {Key = dim}).ToList();
                var paramList = conn.Query<Parameter>("select * from Parameter where Name in @Names", new {Names = new[]{param1, param2}}).ToList();

                return paramList.GroupBy(x => x.TestId).Select(x =>
                {
                    var test = new ExpandoObject() as IDictionary<string, Object>;
                    test["Id"] = x.Key;
                    foreach (var p in x)
                    {
                        test[p.Name] = p.Value;
                    }
                    test["Value"] = valueList.First(v => v.TestId == x.Key).Value;
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