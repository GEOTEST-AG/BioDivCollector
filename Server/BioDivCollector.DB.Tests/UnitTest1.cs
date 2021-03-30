using BioDivCollector.DB.Models.Domain;
using Npgsql;
using NUnit.Framework;
using System.Linq;

namespace BioDivCollector.DB.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            //NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite();
            //public void ConfigureServices(IServiceCollection services)
            //{
            //    services.AddDbContext<BloggingContext>(options => options.UseSqlite("Data Source=blog.db"));
            //}
        }

        [Test]
        public void DbTest1()
        {
   

            using (BioDivContext db = new BioDivContext())
            {
                

                var allGeometries = db.Geometries.ToList();


            }
            Assert.Pass();
        }
    }
}