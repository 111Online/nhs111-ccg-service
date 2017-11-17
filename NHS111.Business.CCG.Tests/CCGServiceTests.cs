
namespace NHS111.Business.CCG.Tests {
    using System;
    using System.Threading.Tasks;
    using Domain.CCG;
    using Moq;
    using NUnit.Framework;
    using Services;
    using Domain.CCG.Models;

    [TestFixture]
    public class CCGServiceTests {
        [SetUp]
        public void SetUp() {
            _mockccgRepo = new Mock<ICCGRepository>();
            _mockstpRepo = new Mock<ISTPRepository>();
            _sut = new CCGService(_mockccgRepo.Object, _mockstpRepo.Object);
        }

        [Test(Description = "The CCG service should throw an ArgumentException if an invalid postcode is provided.")]
        public void Get_WithInvalidPostcode_ThrowsArgumentException() {
            Assert.ThrowsAsync<ArgumentException>(() => _sut.Get(_invalidPostcode), TestContext.CurrentContext.Test.Expectation());
        }

        [Test(Description = "The CCG service should throw an ArgumentException if an invalid postcode is provided.")]
        public void Get_Details_WithInvalidPostcode_ThrowsArgumentException()
        {
            Assert.ThrowsAsync<ArgumentException>(() => _sut.GetDetails(_invalidPostcode), TestContext.CurrentContext.Test.Expectation());
        }

        [Test(Description="The CCG service should return a valid CCG model when one is returned from the repo.")]
        public async Task Get_WithExistingPostcode_ReturnsCCG() {
            var repoResult = SetupMockCCGRepoResult();
            var actualResult = await _sut.Get(_validPostcode);
            Assert.AreEqual(repoResult.Postcode, actualResult.Postcode, TestContext.CurrentContext.Test.Expectation() + " Postcodes didn't match");
            Assert.AreEqual(repoResult.App, actualResult.App, TestContext.CurrentContext.Test.Expectation() + " App didn't match");
            Assert.AreEqual(repoResult.CCG, actualResult.CCG, TestContext.CurrentContext.Test.Expectation() + " CCG didn't match");
        }

        [Test(Description = "The CCG service should return a valid CCG model with details when one is returned from the repo.")]
        public async Task Get_Details_WithExistingPostcode_ReturnsCCGExtendedDetails()
        {
            var ccgRepoResult = SetupMockCCGRepoResult();
            var stpRepoResult = new STPEntity() { CCGId = "1234", ProductName = "some app", STPName = "SomeStp", CCGName = "some ccg", ServiceIdWhitelist = "55555|66666|77777"};
            _mockstpRepo.Setup(r => r.Get(It.IsAny<string>())).Returns(Task.FromResult(stpRepoResult));

            var actualResult = await _sut.GetDetails(_validPostcode);
            Assert.AreEqual(ccgRepoResult.Postcode, actualResult.Postcode, TestContext.CurrentContext.Test.Expectation() + " Postcodes didn't match");
            Assert.AreEqual(ccgRepoResult.App, actualResult.App, TestContext.CurrentContext.Test.Expectation() + " App didn't match");
            Assert.AreEqual(ccgRepoResult.CCG, actualResult.CCG, TestContext.CurrentContext.Test.Expectation() + " CCG didn't match");
            Assert.AreEqual(3, actualResult.ServiceIdWhitelist.Count);
            Assert.IsTrue(actualResult.ServiceIdWhitelist.Contains("66666"));
        }

        private CCGEntity SetupMockCCGRepoResult()
        {
            var repoResult = new CCGEntity {Postcode = "XXX", App = "some app", CCG = "some ccg", CCGId = "1234"};
            _mockccgRepo.Setup(r => r.Get(It.IsAny<string>())).Returns(Task.FromResult(repoResult));
            return repoResult;
        }

        [Test(Description = "The CCG service should return null when null is returned from the repo.")]
        public async Task Get_WithNonexistingPostcode_ReturnsNull() {
            _mockccgRepo.Setup(r => r.Get(It.IsAny<string>())).Returns(Task.FromResult<CCGEntity>(null));
            var actualResult = await _sut.Get(_validPostcode);
            Assert.IsNull(actualResult, TestContext.CurrentContext.Test.Expectation());
        }

        [Test(Description = "The postcodes are uppercase and no white space in the datastore, so the postcodes must be normalised prior to being passed to the repo.")]
        public async Task Get_WithAnyPostcode_NormalisesThePostcodeBeforeCallingRepo() {
            var nonNormalisedPostcode = " sO66  6iI  ";
            await _sut.Get(nonNormalisedPostcode);
            _mockccgRepo.Verify(r => r.Get(It.Is<string>(s => s == "SO666II")));
        }

        private string _validPostcode = "SO66 6UU";
        private string _invalidPostcode = "XXXX XXX";

        private Mock<ICCGRepository> _mockccgRepo;
        private Mock<ISTPRepository> _mockstpRepo;
        private CCGService _sut;
    }
}