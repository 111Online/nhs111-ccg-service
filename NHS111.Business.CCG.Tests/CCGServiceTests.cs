using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NHS111.Business.CCG.Tests
{
    using Domain.CCG;
    using Domain.CCG.Models;

    using Services;

    [TestFixture]
    public class CCGServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            _mockccgRepo = new Mock<ICCGRepository>();
            _mockstpRepo = new Mock<ISTPRepository>();
            _mockAzureAccountSettings = new Mock<IAzureAccountSettings>();
        }

        [Test(Description = "The CCG service should throw an ArgumentException if an invalid postcode is provided.")]
        public void Get_WithInvalidPostcode_ThrowsArgumentException()
        {
            Assert.ThrowsAsync<ArgumentException>(() => CCGService().GetCCGDetails(_invalidPostcode), TestContext.CurrentContext.Test.Expectation());
        }

        [Test(Description = "The CCG service should throw an ArgumentException if an invalid postcode is provided.")]
        public void Get_Details_WithInvalidPostcode_ThrowsArgumentException()
        {
            Assert.ThrowsAsync<ArgumentException>(() => CCGService().GetDetails(_invalidPostcode), TestContext.CurrentContext.Test.Expectation());
        }

        [Test(Description = "The CCG service should return a valid CCG model when one is returned from the repo.")]
        public async Task Get_WithExistingPostcode_ReturnsCCG()
        {
            var ccgRepoResult = SetupMockCCGRepoResult();

            var stpRepoResult = SetupMockSTPRepoResult();

            var actualResult = await CCGService().GetCCGDetails(_validPostcode);

            Assert.AreEqual(ccgRepoResult.Postcode, actualResult.Postcode, TestContext.CurrentContext.Test.Expectation() + " Postcodes didn't match");
            Assert.AreEqual(ccgRepoResult.App, actualResult.App, TestContext.CurrentContext.Test.Expectation() + " App didn't match");
            Assert.AreEqual(ccgRepoResult.CCG, actualResult.CCG, TestContext.CurrentContext.Test.Expectation() + " CCG didn't match");
            Assert.AreEqual(stpRepoResult.STPName, actualResult.STP, TestContext.CurrentContext.Test.Expectation() + " CCG didn't match");
        }

        [Test(Description = "The CCG service should return a valid CCG model with details when one is returned from the repo.")]
        public async Task Get_Details_WithExistingPostcode_ReturnsCCGExtendedDetails()
        {
            // Arrange
            var ccgRepoResult = SetupMockCCGRepoResult();

            var stpEntity = new STPEntity
            {
                ProductName = ccgRepoResult.App,
                ReferralServiceIdWhitelist = "111111|222222|66666"
            };

            _mockstpRepo.Setup(x => x.Get(It.IsAny<string>())).Returns(stpEntity);

            var actualResult = await CCGService().GetDetails(_validPostcode);

            Assert.AreEqual(ccgRepoResult.Postcode, actualResult.Postcode, TestContext.CurrentContext.Test.Expectation() + " Postcodes didn't match");
            Assert.AreEqual(ccgRepoResult.App, actualResult.App, TestContext.CurrentContext.Test.Expectation() + " App didn't match");
            Assert.AreEqual(ccgRepoResult.CCG, actualResult.CCG, TestContext.CurrentContext.Test.Expectation() + " CCG didn't match");
            Assert.AreEqual(3, actualResult.ReferralServiceIdWhitelist.Count);
            Assert.IsTrue(actualResult.ReferralServiceIdWhitelist.Contains("66666"));
            Assert.AreEqual(0, actualResult.PharmacyReferralServiceIdWhitelist.Count);
        }

        private CCGEntity SetupMockCCGRepoResult()
        {
            var repoResult = new CCGEntity { Postcode = "XXX", App = "some app", STP = "some stp", STPId = "5678", CCG = "some ccg", CCGId = "1234" };

            _mockccgRepo.Setup(r => r.Get(It.IsAny<string>())).Returns(Task.FromResult(repoResult));

            return repoResult;
        }

        private STPEntity SetupMockSTPRepoResult()
        {
            var stpRepoResult = new STPEntity { CCGId = "1234", ProductName = "some app", STPName = "SomeStp", CCGName = "some ccg", ReferralServiceIdWhitelist = "55555|66666|77777", PharmacyServiceIdWhitelist = "9999|0000" };

            _mockstpRepo.Setup(r => r.Get(It.IsAny<string>())).Returns(stpRepoResult);

            return stpRepoResult;
        }

        [Test(Description = "The CCG service should return null when null is returned from the repo.")]
        public async Task Get_WithNonexistingPostcode_ReturnsNull()
        {
            _mockccgRepo.Setup(r => r.Get(It.IsAny<string>())).Returns(Task.FromResult<CCGEntity>(null));

            var actualResult = await CCGService().GetCCGDetails(_validPostcode);

            Assert.IsNull(actualResult, TestContext.CurrentContext.Test.Expectation());
        }

        [Test(Description = "The postcodes are uppercase and no white space in the datastore, so the postcodes must be normalised prior to being passed to the repo.")]
        public async Task Get_WithAnyPostcode_NormalisesThePostcodeBeforeCallingRepo()
        {
            var nonNormalisedPostcode = " sO66  6iI  ";

            await CCGService().GetCCGDetails(nonNormalisedPostcode);

            _mockccgRepo.Verify(r => r.Get(It.Is<string>(s => s == "SO666II")));
        }

        private CCGService CCGService()
        {
            return new CCGService(_mockccgRepo.Object, _mockstpRepo.Object, _mockAzureAccountSettings.Object);
        }

        private string _validPostcode = "SO66 6UU";
        private string _invalidPostcode = "XXXX XXX";

        private Mock<IAzureAccountSettings> _mockAzureAccountSettings;
        private Mock<ICCGRepository> _mockccgRepo;
        private Mock<ISTPRepository> _mockstpRepo;
    }
}