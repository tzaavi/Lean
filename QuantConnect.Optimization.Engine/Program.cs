using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Nancy;
using Nancy.Hosting.Self;

namespace QuantConnect.Optimization.Engine
{
    class Program
    {
        static void Main(string[] args)
        {
            var db = new SQLiteConnection("URI=file:algo.db");

            /*db.Open();

            var cmd = db.CreateCommand();
            cmd.CommandText = @"
                create table if not exists ChartPoint (
                    PermutationId int,
                    Time datetime,
                    Value double
                );
            ";
            cmd.ExecuteNonQuery();


            db.Close();*/

            db.Insert(new ChartPoint
            {
                PermutationId = 14,
                Time = DateTime.Now,
                Value = 11.22m
            });


            var res = db.Query<ChartPoint>("select * from ChartPoint where PermutationId = 14");
            //var res = db.Query<ChartPoint>("select PermutationId = @PermutationId", new { PermutationId = 12});

           
        }

        static void StartNancy()
        {
            // need to run this comand in cmd (admin) 
            // netsh http add urlacl url=http://+:8888/nancy/ user=Everyone
            using (var nancyHost = new NancyHost(new Uri("http://localhost:8888/nancy/")))
            {
                nancyHost.Start();

                Console.WriteLine("Nancy now listening - navigating to http://localhost:8888/nancy/. Press enter to stop");
                try
                {
                    Process.Start("http://localhost:8888/nancy/");
                }
                catch (Exception)
                {
                }
                Console.ReadKey();
            }

            Console.WriteLine("Stopped. Good bye!");
        }
    }

    public class TestModule : NancyModule
    {
        public TestModule()
        {
            Get["/"] = parameters =>
            {
                return View["index", this.Request.Url];
            };

            Get["/testing"] = parameters =>
            {
                return View["staticview", this.Request.Url];
            };
        }
    }

    [Table("ChartPoint")]
    public class ChartPoint
    {
        public int PermutationId { get; set; }
        public DateTime Time { get; set; }
        public decimal Value { get; set; }
    }
}
