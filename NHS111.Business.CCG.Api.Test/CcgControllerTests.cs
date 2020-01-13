namespace NHS111.Business.CCG.Api.Test {
    using System.Threading.Tasks;
    using Controllers;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Moq;
    using NUnit.Framework;
    using Services;

    [TestFixture]
    public class CCGControllerTests {

        [SetUp]
        public void SetUp() {
            _mockService = new Mock<ICCGService>();
        }

        [Test(Description = "When calling Get() without a postcode it should respond with a 400 Bad Request result.")]
        public async Task Get_WithNoPostcode_Returns400BadReqeust() {
            //Arrange
            var sut = new CCGController(_mockService.Object);
            //Act
            var response = await sut.Get(null);
            //Assert
            Assert.IsInstanceOf<BadRequestResult>(response, TestContext.CurrentContext.Test.Expectation());
            response = await sut.Get("");
            Assert.IsInstanceOf<BadRequestResult>(response, TestContext.CurrentContext.Test.Expectation());
        }

        [Test(Description = "When calling Get() with a CCG returned from datalayer it should respond with a 200 OK result and correct CCG.")]
        public async Task Get_WhenCCGReturnedFromDataLayer_Returns200OK() {
            //Arrange
            var expectedCCG = new CCGModel { Postcode = "So302un"};
            _mockService.Setup(s => s.Get(It.IsAny<string>())).Returns(Task.FromResult(expectedCCG));
            var sut = new CCGController(_mockService.Object);
            //Act
            var response = await sut.Get("So302un");
            //Assert
            Assert.IsInstanceOf<OkObjectResult>(response, TestContext.CurrentContext.Test.Expectation());
            var model = (response as OkObjectResult)?.Value;
            Assert.IsInstanceOf<CCGModel>(model, TestContext.CurrentContext.Test.Expectation());
            Assert.AreEqual(expectedCCG.Postcode, ((CCGModel)model).Postcode, TestContext.CurrentContext.Test.Expectation());
        }

        [Test(Description = "When calling Get() with a CCG NOT returned from datalayer it should respond with a 404 Not Found result.")]
        public async Task Get_WhenCCGNotReturnedFromDataLayer_Returns404NotFound() {
            //Arrange
            _mockService.Setup(s => s.Get(It.IsAny<string>())).Returns(Task.FromResult<CCGModel>(null));
            var sut = new CCGController(_mockService.Object);
            //Act
            var response = await sut.Get("So302un");
            //Assert
            Assert.IsInstanceOf<NotFoundResult>(response, TestContext.CurrentContext.Test.Expectation());
        }

        private Mock<ICCGService> _mockService;
    }
}