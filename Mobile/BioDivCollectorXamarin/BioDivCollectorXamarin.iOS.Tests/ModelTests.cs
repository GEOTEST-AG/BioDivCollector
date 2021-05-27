using System;
using NUnit.Framework;
namespace BioDivCollectorXamarin.iOS.Tests
{
    [TestFixture]
    public class ModelTests
    {
        
        [SetUp]
        public void Setup()
        {

        }


        [TearDown]
        public void Tear()
        {

        }

        [Test]
        public void Test1()
        {
            string json = "{ \"projectId\": \"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"startDateTime\": \"2021-05-21T12:15:39.961Z\",\"projectName\": \"string\",\"description\": \"string\",\"projectNumber\": \"string\",\"id_Extern\": \"string\",\"projectStatusId\": 1,\"projectManager\": \"string\",\"projectConfigurator\": \"string\",\"geometries\": [{\"geometryId\": \"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"geometryName\": \"string\",\"geometry\": \"string\",\"userName\": \"string\",\"fullUserName\": \"string\",\"timestamp\": \"2021-05-21T12:15:39.962Z\",\"creationTime\": \"2021-05-21T12:15:39.962Z\",\"status\": 0,\"readOnly\": true,\"records\": [{\"recordId\": \"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"formId\": 0,\"userName\": \"string\",\"fullUserName\": \"string\",\"timestamp\": \"2021-05-21T12:15:39.962Z\",\"creationTime\": \"2021-05-21T12:15:39.962Z\",\"status\": 0,\"readOnly\": true,\"texts\": [{\"textId\": \"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"title\": \"string\",\"value\": \"string\",\"formFieldId\": 0,\"fieldChoiceId\": 0}],\"numerics\": [{\"numericId\": \"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"title\": \"string\",\"value\": 0,\"formFieldId\": 0}],\"booleans\": [{\"booleanId\": \"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"title\": \"string\",\"value\": true,\"formFieldId\": 0}]}]}],\"records\": [{\"recordId\": \"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"formId\": 0,\"userName\": \"string\",\"fullUserName\": \"string\",\"timestamp\": \"2021-05-21T12:15:39.962Z\",\"creationTime\": \"2021-05-21T12:15:39.962Z\",\"status\": 0,\"readOnly\": true,\"texts\": [{\"textId\": \"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"title\": \"string\",\"value\": \"string\",\"formFieldId\": 0,\"fieldChoiceId\": 0}],\"numerics\": [{\"numericId\": \"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"title\": \"string\",\"value\": 0,\"formFieldId\": 0}],\"booleans\": [{\"booleanId\": \"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"title\": \"string\",\"value\": true,\"formFieldId\": 0}]}],\"forms\": [{\"formId\": 0,\"title\": \"string\",\"timestamp\": \"2021-05-21T12:15:39.962Z\",\"formFields\": [{\"fieldId\": 0,\"typeId\": 0,\"title\": \"string\",\"description\": \"string\",\"source\": \"string\",\"order\": 0,\"mandatory\": true,\"useInRecordTitle\": true}]}],\"layers\": [{\"layerId\": 0,\"title\": \"string\",\"url\": \"string\",\"wmsLayer\": \"string\",\"visible\": true,\"opacity\": 0,\"order\": 0}]}";
            Assert.IsTrue(true);
        }


    }
}
