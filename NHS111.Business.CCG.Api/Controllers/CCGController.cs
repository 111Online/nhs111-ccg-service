namespace NHS111.Business.CCG.Api.Controllers {
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Services;

    [Route("api/[controller]")]
    public class CCGController
        : Controller {

        public CCGController(ICCGService service) {
            _service = service;
        }

        [HttpGet("{postcode}")]
        public async Task<IActionResult> Get(string postcode) {
            if (IsBadRequest(postcode))
                return BadRequest();

            var result = await _service.Get(postcode);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("List")]
        public async Task<IActionResult> List() {

            var result = await _service.List();

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("Details/{postcode}")]
        public async Task<IActionResult> GetDetails(string postcode) {
            if (IsBadRequest(postcode))
                return BadRequest();

            var result = await _service.GetDetails(postcode);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        private static bool IsBadRequest(string postcode) {
            return !PostCodeFormatValidator.IsAValidPostcode(postcode) ||
                   string.IsNullOrEmpty(postcode);
        }

        private readonly ICCGService _service;
    }
}