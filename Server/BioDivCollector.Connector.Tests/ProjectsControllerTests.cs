//using BioDivCollector.Connector.Controllers;
//using BioDivCollector.DB.Models.Domain;
//using Microsoft.Extensions.DependencyInjection;
//using NetTopologySuite.Geometries;
//using NUnit.Framework;
//using System;
//using System.Linq;

//namespace BioDivCollector.Connector.Tests
//{
//    public class ProjectsControllerTests
//    {
//        private ProjectController projectController;
//        private BioDivContext context;

//        [SetUp]
//        public void Setup()
//        {
//            context = new BioDivContext();
//            projectController = new ProjectController(context);
//        }

//        /// <summary>
//        /// Create project, delete project
//        /// </summary>
//        [Test]
//        public void TestCreateDeleteProject()
//        {
//            var rng = new Random();

//            var newProject = new Project
//            {
//                ProjectId = Guid.NewGuid(),
//                ProjectName = DateTime.Now.ToString(),
//                ProjectNumber = rng.Next(10000),
//                StatusId = StatusEnum.unchanged,
//                ProjectStatusId = ProjectStatusEnum.Projekt_bereit
//            };

//            var newGeometry1 = new ReferenceGeometry()
//            {
//                GeometryId = Guid.NewGuid(),
//                GeometryName = DateTime.Now.ToLongTimeString(),
//                StatusId = StatusEnum.unchanged,
//                Point = new Point(7.72524 + rng.NextDouble() * 0.001, 46.483535 + rng.NextDouble() * 0.001) { SRID = 3857 },
//            };

//            var newGeometry2 = new ReferenceGeometry()
//            {
//                GeometryId = Guid.NewGuid(),
//                GeometryName = DateTime.Now.ToLongTimeString(),
//                StatusId = StatusEnum.unchanged,
//                Line = new LineString(
//                    new Coordinate[] {
//                        new Coordinate(7.3402040464418405 + rng.NextDouble() * 0.001, 46.84825809689678 + rng.NextDouble() * 0.001),
//                        new Coordinate(7.340438739737891 + rng.NextDouble() * 0.001, 46.848823104831176+ rng.NextDouble() * 0.001) }
//                    )
//            };

//            var polygonShell = new LinearRing(new Coordinate[] {
//                new Coordinate(7.4594780000000185,46.99892800000001),
//                new Coordinate(7.459716000000043,46.999207),
//                new Coordinate(7.459857000000028,46.999161999999984),
//                new Coordinate(7.459630000000004,46.998884),
//                new Coordinate(7.4594780000000185,46.99892800000001)}
//            );
//            var newGeometry3 = new ReferenceGeometry()
//            {
//                GeometryId = Guid.NewGuid(),
//                GeometryName = DateTime.Now.ToLongTimeString(),
//                StatusId = StatusEnum.unchanged,
//                Polygon = new Polygon(polygonShell)
//            };

//            newProject.Geometries.Add(newGeometry1);
//            newProject.Geometries.Add(newGeometry2);
//            newProject.Geometries.Add(newGeometry3);

//            projectController.PostProject(newProject).Wait();

//            bool checkProjectExists = context.Projects.Any(e => e.ProjectId == newProject.ProjectId);

//            Assert.IsTrue(checkProjectExists);

//            //----------------------------------------------------------------------------------------------------
//            //----------------------------------------------------------------------------------------------------

//            projectController.DeleteProject(newProject.ProjectId).Wait();

//            Project projectAgain = context.Projects.Find(newProject.ProjectId);

//            Assert.AreEqual(StatusEnum.deleted, projectAgain.StatusId);



//            //Assert.Pass();
//        }
//    }
//}