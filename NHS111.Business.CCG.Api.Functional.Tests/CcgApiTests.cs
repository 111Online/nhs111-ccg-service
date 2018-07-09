using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NHS111.Business.CCG.Functional.Tests.Tools;
using NHS111.Business.CCG.Models;
using NUnit.Framework;
using RestSharp;

namespace NHS111.Business.CCG.Api.Functional.Tests
{
    [TestFixture]
    public class CcgApiTests
    {
        private IConfiguration _config;
        private IRestClient _restClient;

        private string CcgApiGetUrl => string.Format("{0}{1}", _config["CcgApiProtocolandDomain"], _config["CcgApiGetUrl"]);

        private string CcgApiGetDetailsUrl => string.Format("{0}{1}", _config["CcgApiProtocolandDomain"], _config["CcgApiGetDetailsUrl"]);

        [SetUp]
        public void SetUp()
        {
            _config = ConfigurationHelper.GetIConfigurationRoot(Directory.GetCurrentDirectory());
            _restClient = new RestClient(_config["CcgApiProtocolandDomain"]);
        }

        [Test]
        public async Task CcgApiGetTests_returns_valid_response()
        {
            var result = await _restClient.ExecuteTaskAsync<CCGModel>(new RestRequest(string.Format(_config["CcgApiGetUrl"], "so302un")));

            Assert.IsNotNull(result.Data);
            SchemaValidation.AssertValidResponseSchema(JsonConvert.SerializeObject(result.Data), SchemaValidation.ResponseSchemaType.Get);
        }

        [TestCase("AL1 1DU", 25)]
        [TestCase("HA1 3SW", 15)]
        public async Task CcgApiGetTests_returns_correct_searchdistance_postcode(string postcode, int searchDistance)
        {
            var result = await _restClient.ExecuteTaskAsync<CCGModel>(new RestRequest(string.Format(_config["CcgApiGetDetailsUrl"], postcode)));

            Assert.IsNotNull(result.Data);
            Assert.AreEqual(result.Data.SearchDistance, searchDistance);
        }

        [Test]
        public async Task CcgApiGetDetailsTests_returns_valid_response()
        {
            var result = await _restClient.ExecuteTaskAsync<CCGModel>(new RestRequest(string.Format(_config["CcgApiGetDetailsUrl"], "so302un")));

            Assert.IsNotNull(result.Data);
            SchemaValidation.AssertValidResponseSchema(JsonConvert.SerializeObject(result.Data), SchemaValidation.ResponseSchemaType.GetDetails);
        }
    }
}
