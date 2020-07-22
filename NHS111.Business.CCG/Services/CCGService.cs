using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NHS111.Business.CCG.Services
{
    using Domain.CCG;
    using Domain.CCG.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Models;
    using System.IO;

    public class CCGService : ICCGService
    {
        private const string WhitelistBlobContainerName = "epwhitelist";

        private CloudBlobContainer _container;

        public CCGService(ICCGRepository ccgRepository, ISTPRepository stpRepository, IAzureAccountSettings azureAccountSettings)
        {
            _ccgRepository = ccgRepository;
            _stpRepository = stpRepository;
            _azureAccountSettings = azureAccountSettings;
        }

        public async Task<CCGModel> GetCCGDetails(string postcode)
        {
            if (!PostCodeFormatValidator.IsAValidPostcode(postcode))
            {
                throw new ArgumentException("Postcode must be valid.");
            }

            postcode = NormalisePostcode(postcode);

            var ccgResult = await _ccgRepository.Get(postcode);

            if (ccgResult == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(ccgResult.CCGId))
            {
                throw new ArgumentOutOfRangeException("Postcode does not have CCGId specified", new Exception(""));
            }

            var stpResult = await _stpRepository.Get(ccgResult.CCGId);

            return MapCcgToStp(ccgResult, stpResult);
        }

        private string NormalisePostcode(string postcode)
        {
            return postcode.Replace(" ", "").ToUpper();
        }

        private CCGModel MapCcgToStp(CCGEntity ccg, STPEntity stp)
        {
            if (ccg == null || stp == null)
            {
                return null;
            }

            return new CCGModel
            {
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
            {
                return null;
            }

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
            {
                return null;
            }

            return new CCGDetailsModel
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
            {
                throw new ArgumentException("Postcode must be valid.");
            }

            postcode = NormalisePostcode(postcode);

            var ccgResult = await _ccgRepository.Get(postcode);

            if (ccgResult == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(ccgResult.CCGId))
            {
                throw new ArgumentOutOfRangeException("Postcode does not have CCGId specified", new Exception(""));
            }

            var stpResult = await _stpRepository.Get(ccgResult.CCGId);

            if (stpResult != null && !string.IsNullOrWhiteSpace(stpResult.PharmacyServiceIdWhitelist))
            {
                stpResult.PharmacyServiceIdWhitelist = await AppendNationalWhitelistToGPOutOfHours(stpResult.PharmacyServiceIdWhitelist);
            }

            return DetailsMap(ccgResult, stpResult);
        }

        private string nationalWhitelist = null;

        private async Task<string> AppendNationalWhitelistToGPOutOfHours(string gpOutOfHours)
        {
            // Download the whitelist file only once
            if (nationalWhitelist == null)
            {
                var blob = GetBlobReference(_azureAccountSettings.NationalWhitelistBlobName);
                nationalWhitelist = await blob.DownloadTextAsync();
            }
            return string.Join('|', nationalWhitelist, gpOutOfHours);
        }

        private CloudBlockBlob GetBlobReference(string name)
        {
            try
            {
                if (_container == null)
                {
                    var storageAccount = CloudStorageAccount.Parse(_azureAccountSettings.ConnectionString);
                    var client = storageAccount.CreateCloudBlobClient();
                    client.DefaultRequestOptions.LocationMode = _azureAccountSettings.LocationMode;

                    _container = client.GetContainerReference(WhitelistBlobContainerName);
                }
                var blob = _container.GetBlockBlobReference(name);

                return blob;
            }
            catch (Exception e)
            {
                // TODO: Add application logging

                throw new Exception("", e);
            }
        }

        public async Task<List<CCGSummaryModel>> List()
        {
            var ccgResult = await _stpRepository.List();

            return ccgResult.Select(SummaryMap).ToList();
        }

        private readonly IAzureAccountSettings _azureAccountSettings;
        private readonly ICCGRepository _ccgRepository;
        private readonly ISTPRepository _stpRepository;
    }
}