
using System.Collections.Generic;
using System.Linq;

namespace NHS111.Business.CCG.Services {
    using System;
    using System.Threading.Tasks;
    using Domain.CCG;
    using Domain.CCG.Models;
    using Models;

    public interface ICCGService {
        Task<CCGModel> Get(string postcode);
        Task<CCGDetailsModel> GetDetails(string postcode);
        Task<List<CCGSummaryModel>> List();
    }

    public class CCGService
        : ICCGService {

        public CCGService(ICCGRepository ccgRepository, ISTPRepository stpRepository) {
            _ccgRepository = ccgRepository;
            _stpRepository = stpRepository;
        }

        public async Task<CCGModel> Get(string postcode) {
            if (!PostCodeFormatValidator.IsAValidPostcode(postcode))
                throw new ArgumentException("Postcode must be valid.");

            postcode = NormalisePostcode(postcode);
            var ccgResult = await _ccgRepository.Get(postcode);

            if (ccgResult == null)
                return null;

            if (String.IsNullOrEmpty(ccgResult.CCGId)) throw new ArgumentOutOfRangeException("Postcode does not have CCGId specified");
            var stpResult = await _stpRepository.Get(ccgResult.CCGId);
            return Map(ccgResult, stpResult);
        }

        private string NormalisePostcode(string postcode) {
            return postcode.Replace(" ", "").ToUpper();
        }

        private CCGModel Map(CCGEntity ccg, STPEntity stp) {
            if (ccg == null || stp == null)
                return null;

            return new CCGModel {
                Postcode = ccg.Postcode,
                STP = stp.STPName,
                CCG = ccg.CCG,
                App = ccg.App,
                DOSSearchDistance = ccg.DOSSearchDistance
            };
        }


        private CCGSummaryModel SummaryMap(STPEntity result)
        {
            if (result == null)
                return null;

            return new CCGSummaryModel
            {      
                CCG = result.CCGName,  
                CCGId = result.CCGId,
                STP = result.STPName,
                STPId = result.STPId
            };
        }

        private CCGDetailsModel DetailsMap(CCGEntity ccgEntity, STPEntity stpEntity)
        {
            if (ccgEntity == null || stpEntity == null)
                return null;

            return new CCGDetailsModel()
            {
                App = stpEntity.ProductName,
                CCG = ccgEntity.CCG,
                Postcode = ccgEntity.Postcode,
                ReferralServiceIdWhitelist = new ServiceListModel(stpEntity.ReferralServiceIdWhitelist),
                PharmacyReferralServiceIdWhitelist = new ServiceListModel(stpEntity.PharmacyServiceIdWhitelist),
                STPName = stpEntity.STPName,
                DOSSearchDistance = ccgEntity.DOSSearchDistance
            };
        }

        public async Task<CCGDetailsModel> GetDetails(string postcode)
        {
            if (!PostCodeFormatValidator.IsAValidPostcode(postcode))
                throw new ArgumentException("Postcode must be valid.");

            postcode = NormalisePostcode(postcode);
            var ccgResult = await _ccgRepository.Get(postcode);
            STPEntity stpResult = new STPEntity();
            if (ccgResult == null)
                return null;

            if(String.IsNullOrEmpty(ccgResult.CCGId)) throw new ArgumentOutOfRangeException("Postcode does not have CCGId specified");
            stpResult = await _stpRepository.Get(ccgResult.CCGId);
            return DetailsMap(ccgResult, stpResult);
        }

        public async Task<List<CCGSummaryModel>> List()
        {
            var ccgResult = await _stpRepository.List();
            return ccgResult.Select(SummaryMap).ToList();
        }

        private readonly ICCGRepository _ccgRepository;
        private readonly ISTPRepository _stpRepository;
    }
}