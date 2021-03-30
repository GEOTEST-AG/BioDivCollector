//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using BioDivCollector.DB.Models.Domain;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using NetTopologySuite.Geometries;

//namespace BioDivCollector.Connector.Controllers
//{
//    [ApiController]
//    [Route("[controller]")]
//    public class TestController : ControllerBase
//    {
//        private readonly ILogger<TestController> _logger;
//        private readonly BioDivContext _context;

//        public TestController(ILogger<TestController> logger, BioDivContext context)
//        {
//            _logger = logger;
//            _context = context;
//        }

//        /// <summary>
//        /// 1. Create dummy project with geometries and records
//        /// 2. Show all projects in the browser.
//        /// </summary>
//        /// <returns></returns>
//        [HttpGet("{create?}")]
//        [Produces("application/json")]
//        public IEnumerable<Project> NewProjectList(bool create = false)
//        {
//            //var projects = _context.Projects.ToList();
//            //return projects.ToArray();

//            if (create)
//            {
//                var rng = new Random();

//                var newProject = new Project
//                {
//                    ProjectId = Guid.NewGuid(),
//                    ProjectName = DateTime.Now.ToString(),
//                    ProjectNumber = rng.Next(10000),
//                    StatusId = StatusEnum.unchanged,
//                    ProjectStatusId = ProjectStatusEnum.Projekt_bereit
//                };

//                var newGeometry1 = new ReferenceGeometry()
//                {
//                    GeometryId = Guid.NewGuid(),
//                    GeometryName = DateTime.Now.ToLongTimeString(),
//                    StatusId = StatusEnum.unchanged,
//                    Point = new Point(7.72524 + rng.NextDouble() * 0.001, 46.483535 + rng.NextDouble() * 0.001) { SRID = 4326 },
//                };

//                var newGeometry2 = new ReferenceGeometry()
//                {
//                    GeometryId = Guid.NewGuid(),
//                    GeometryName = DateTime.Now.ToLongTimeString(),
//                    StatusId = StatusEnum.unchanged,
//                    Line = new LineString(
//                        new Coordinate[] {
//                        new Coordinate(7.3402040464418405 + rng.NextDouble() * 0.001, 46.84825809689678 + rng.NextDouble() * 0.001),
//                        new Coordinate(7.340438739737891 + rng.NextDouble() * 0.001, 46.848823104831176+ rng.NextDouble() * 0.001) }
//                        )
//                    { SRID = 4326 }
//                };

//                var polygonShell = new LinearRing(new Coordinate[] {
//                new Coordinate(7.4594780000000185,46.99892800000001),
//                new Coordinate(7.459716000000043,46.999207),
//                new Coordinate(7.459857000000028,46.999161999999984),
//                new Coordinate(7.459630000000004,46.998884),
//                new Coordinate(7.4594780000000185,46.99892800000001)}
//                );
//                var newGeometry3 = new ReferenceGeometry()
//                {
//                    GeometryId = Guid.NewGuid(),
//                    GeometryName = DateTime.Now.ToLongTimeString(),
//                    StatusId = StatusEnum.unchanged,
//                    Polygon = new Polygon(polygonShell) { SRID = 4326 }
//                };

//                newProject.Geometries.Add(newGeometry1);
//                newProject.Geometries.Add(newGeometry2);
//                newProject.Geometries.Add(newGeometry3);

//                var newRecord1 = new Record()
//                {
//                    RecordId = Guid.NewGuid(),
//                    StatusId = StatusEnum.unchanged,
//                };
//                var newRecord2 = new Record()
//                {
//                    RecordId = Guid.NewGuid(),
//                    StatusId = StatusEnum.unchanged,
//                };
//                var newRecord3 = new Record()
//                {
//                    RecordId = Guid.NewGuid(),
//                    StatusId = StatusEnum.unchanged,
//                };
//                newProject.Records.Add(newRecord1);
//                newProject.Records.Add(newRecord2);
//                newProject.Records.Add(newRecord3);

//                newRecord1 = new Record()
//                {
//                    RecordId = Guid.NewGuid(),
//                    StatusId = StatusEnum.unchanged,
//                };
//                newRecord2 = new Record()
//                {
//                    RecordId = Guid.NewGuid(),
//                    StatusId = StatusEnum.unchanged,
//                };
//                newRecord3 = new Record()
//                {
//                    RecordId = Guid.NewGuid(),
//                    StatusId = StatusEnum.unchanged,
//                };
//                newGeometry1.Records.Add(newRecord1);
//                newGeometry1.Records.Add(newRecord2);
//                newGeometry1.Records.Add(newRecord3);

//                _context.Projects.Add(newProject);
//                _context.SaveChanges();
//            }

//            var projects = _context.Projects
//                //.Include(p => p.Geometries)
//                .ToList();
//            return projects.ToArray();

//            //return Enumerable.Range(1, 5).Select(index => new Project
//            //{
//            //    ProjectId = Guid.NewGuid(),
//            //    ProjectName = Summaries[rng.Next(Summaries.Count)],
//            //    ProjectNumber = rng.Next(10000),
//            //    StatusId = StatusEnum.unchanged,
//            //    Status_ProjectId = ProjectStatusEnum.Projekt_bereit
//            //})
//            //.ToArray();
//        }
//    }
//}
