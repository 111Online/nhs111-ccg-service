using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NHS111.Business.CCG.Functional.Tests.Tools;
using NHS111.Business.CCG.Models;
using NUnit.Framework;

namespace NHS111.Business.CCG.Api.Functional.Tests
{
    [TestFixture]
    public class CcgApiTests
    {
        private HttpClient _httpClient;

        [SetUp]
        public void SetUp()
        {
            _httpClient = new HttpClient();
            var address = Environment.GetEnvironmentVariable("CcgApiFunctionalTestsBaseUrl");

            if (string.IsNullOrWhiteSpace(address))
            {
                address = "https://111-int360-ukw-ccg-api.azurewebsites.net/";
            }

            _httpClient.BaseAddress = new Uri(address);
        }

        [Test]
        public async Task CcgApiGetTests_returns_valid_response()
        {
            //var result = await _restClient.ExecuteTaskAsync(new RestRequest(string.Format(_config["CcgApiGetUrl"], "so302un")));
            var response = await _httpClient.GetAsync(string.Format("api/ccg/{0}", "so302un"));
            var result = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.IsNotNull(result);
            SchemaValidation.AssertValidResponseSchema(result, SchemaValidation.ResponseSchemaType.Get);
        }

        [TestCase("AL1 1DU", "25")]
        [TestCase("HA1 3SW", "15")]
        [TestCase("NN11 0PZ", "40")]
        //full postcode with partial defined for same area
        [TestCase("BR6 0AB", "20")]
        [TestCase("DA14 6PS", "17")]
        //partial postcode defined, not specific postcode
        [TestCase("BR8 7BU", "21")]
        [TestCase("RH16 1AA", "24")]
        //neither full or partial postcode defined
        [TestCase("SO30 2UN", "18")]
        public async Task CcgApiGetTests_returns_correct_searchdistance_postcode(string postcode, string searchDistance)
        {
            var response = await _httpClient.GetAsync($"api/ccg/{postcode}");
            var result = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.IsNotNull(result);
            var data = JsonConvert.DeserializeObject<CCGModel>(result);
            Assert.AreEqual(searchDistance, data.DOSSearchDistance);
        }

        [Test]
        public async Task CcgApiGetDetailsTests_returns_valid_response()
        {
            var response = await _httpClient.GetAsync($"api/ccg/details/{"so302un"}");
            var result = await response.Content.ReadAsStringAsync();

            Assert.IsNotNull(result);
            SchemaValidation.AssertValidResponseSchema(result, SchemaValidation.ResponseSchemaType.GetDetails);
        }
    }
}
