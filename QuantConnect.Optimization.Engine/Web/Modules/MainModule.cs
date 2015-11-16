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

            Get["/executions/{id}"] = parameters =>
            {
                return View["Execution"];
            };

            Get["/api/executions"] = parameters =>
            {
                var conn = _db.Connection();
                return conn.Query<Execution>("select * from Execution");
            };

            /*Get["/api/optimization/parameters"] = parameters =>
            {
                var conn = _db.Connection();
                return conn.Query("select distinct(Name) from Parameter");
            };

            Get["/api/optimization/dimentions"] = parameters =>
            {
                var conn = _db.Connection();
                return conn.Query("select Key, Name, Category from Stat group by Key, Name, Category");
            };*/

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