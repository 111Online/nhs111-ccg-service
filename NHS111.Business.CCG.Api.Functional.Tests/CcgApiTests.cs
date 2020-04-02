using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NHS111.Business.CCG.Functional.Tests.Tools;
using NHS111.Business.CCG.Models;
using NUnit.Framework;
using RestSharp;
using System.IO;
using System.Threading.Tasks;

namespace NHS111.Business.CCG.Api.Functional.Tests
{
    [TestFixture]
    public class CcgApiTests
    {
        private IConfiguration _config;
        private IRestClient _restClient;

        [SetUp]
        public void SetUp()
        {
            _config = ConfigurationHelper.GetIConfigurationRoot(Directory.GetCurrentDirectory());
            _restClient = new RestClient(_config["CcgApiProtocolandDomain"]);
        }

        [Test]
        public async Task CcgApiGetTests_returns_valid_response()
        {
            var result = await _restClient.ExecuteTaskAsync(new RestRequest(string.Format(_config["CcgApiGetUrl"], "so302un")));

            Assert.IsNotNull(result.Content);
            SchemaValidation.AssertValidResponseSchema(result.Content, SchemaValidation.ResponseSchemaType.Get);
        }

        [TestCase("AL1 1DU", "25")]
        [TestCase("HA1 3SW", "15")]
        [TestCase("NN11 0PZ", "40")]
        //full postcode with partial defined for same area
        [TestCase("BR6 0AB", "17")]
        [TestCase("DA14 6PS", "17")]
        //partial postcode defined, not specific postcode
        [TestCase("BR8 7BU", "21")]
        [TestCase("RH16 1AA", "24")]
        //neither full or partial postcode defined
        [TestCase("SO30 2UN", "")]
        public async Task CcgApiGetTests_returns_correct_searchdistance_postcode(string postcode, string searchDistance)
        {
            var result = await _restClient.ExecuteTaskAsync(new RestRequest(string.Format(_config["CcgApiGetUrl"], postcode)));

            Assert.IsNotNull(result.Content);
            var data = JsonConvert.DeserializeObject<CCGModel>(result.Content);
            Assert.AreEqual(searchDistance, data.DOSSearchDistance);
        }

        [Test]
        public async Task CcgApiGetDetailsTests_returns_valid_response()
        {
            var result = await _restClient.ExecuteTaskAsync(new RestRequest(string.Format(_config["CcgApiGetDetailsUrl"], "so302un")));

            Assert.IsNotNull(result.Content);
            SchemaValidation.AssertValidResponseSchema(result.Content, SchemaValidation.ResponseSchemaType.GetDetails);
        }
    }
}
