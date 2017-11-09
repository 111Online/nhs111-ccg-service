
namespace NHS111.Business.CCG.Services {
    using System;
    using System.Threading.Tasks;
    using Domain.CCG;
    using Domain.CCG.Models;
    using Models;

    public interface ICCGService {
        Task<CCGModel> Get(string postcode);
    }

    public class CCGService
        : ICCGService {

        public CCGService(ICCGRepository ccgRepository) {
            _ccgRepository = ccgRepository;
        }

        public async Task<CCGModel> Get(string postcode) {
            if (!PostCodeFormatValidator.IsAValidPostcode(postcode))
                throw new ArgumentException("Postcode must be valid.");

            postcode = NormalisePostcode(postcode);
            var result = await _ccgRepository.Get(postcode);

            return Map(result);
        }

        private string NormalisePostcode(string postcode) {
            return postcode.Replace(" ", "").ToUpper();
        }

        private CCGModel Map(CCGEntity result) {
            if (result == null)
                return null;

            return new CCGModel {
                Postcode = result.Postcode,
                CCG = result.CCG,
                App = result.App
            };
        }

        private readonly ICCGRepository _ccgRepository;
    }
}