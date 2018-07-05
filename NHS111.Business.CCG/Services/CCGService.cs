﻿
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


        private CCGSummaryModel Map(STPEntity result)
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

        private CCGDetailsModel Map(CCGEntity ccgEntity, STPEntity stpEntity)
        {
            if (ccgEntity == null)
                return null;

            return new CCGDetailsModel()
            {
                App = stpEntity.ProductName,
                CCG = ccgEntity.CCG,
                Postcode = ccgEntity.Postcode,
                ServiceIdWhitelist = new ServiceListModel(stpEntity.ServiceIdWhitelist),
                ITKServiceIdWhitelist = new ServiceListModel(stpEntity.ITKServiceIdWhitelist),
                STPName = stpEntity.STPName
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
            return Map(ccgResult, stpResult);
        }

        public async Task<List<CCGSummaryModel>> List()
        {
            var ccgResult = await _stpRepository.List();
            return ccgResult.Select(r => Map(r)).ToList();
        }

        private readonly ICCGRepository _ccgRepository;
        private readonly ISTPRepository _stpRepository;
    }
}