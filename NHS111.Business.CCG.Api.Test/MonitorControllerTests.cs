namespace NHS111.Business.CCG.Api.Test
{
    using System;
    using NHS111.Business.CCG.Api.Services;
    using Moq;
    using NHS111.Business.CCG.Api.Controllers;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    class MonitorControllerTests
    {
        private Mock<IMonitorService> _mockService;

        [SetUp]
        public void SetUp()
        {
            _mockService = new Mock<IMonitorService>();
        }


        [Test(Description = "When calling MonitorPing() with no service it should return null")]
        public void MonitorPing_EmptyString_null()
        {
            //Arrange
            var sut = new MonitorController(_mockService.Object);
            //Act
            var response = sut.MonitorPing("");
            //Assert

            Assert.AreEqual(null, response.Result);
        }

        [Test(Description = "When calling MonitorPing() with ping it should return pong")]
        public void MonitorPing_Ping_pong()
        {
            //Arrange
            var sut = new MonitorController(_mockService.Object);
            _mockService.Setup(s => s.Ping()).Returns("pong");
            //Act
            var response = sut.MonitorPing("ping".ToString());
            //Assert
            Assert.AreEqual("pong", response.Result);
        }

        [Test(Description = "When calling MonitorPing() with metrics it should return Metrics")]
        public void MonitorPing_Metrics_Metrics()
        {
            //Arrange
            var sut = new MonitorController(_mockService.Object);
            _mockService.Setup(s => s.Metrics()).Returns("Metrics");
            //Act
            var response = sut.MonitorPing("metrics".ToString());
            //Assert
            Assert.AreEqual("Metrics", response.Result);
        }

        [Test(Description = "When calling MonitorPing() with health it should return True")]
        public void MonitorPing_Health_True()
        {
            //Arrange
            var sut = new MonitorController(_mockService.Object);
            _mockService.Setup(s => s.Health()).Returns(Task.FromResult(true));
            //Act
            var response = sut.MonitorPing("health".ToString());
            //Assert
            Assert.AreEqual("True", response.Result);
        }
    }
}
