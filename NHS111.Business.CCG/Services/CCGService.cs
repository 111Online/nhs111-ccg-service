using System.Collections.Generic;
using System.Linq;

using System;
using System.Threading.Tasks;

namespace NHS111.Business.CCG.Services
{
    using System.IO;

    using Domain.CCG;
    using Domain.CCG.Models;

    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    using Models;

    public class CCGService : ICCGService
    {
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

        private async Task<string> AppendNationalWhitelistToGPOutOfHours(string gpOutOfHours)
        {
            var blob = GetBlob(_azureAccountSettings.NationalWhitelistBlobName + ".csv").Result;

            var allServices = new List<string>();

            using (var ms = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(ms);

                ms.Position = 0;

                using (var sr = new StreamReader(ms))
                {
                    var nationalWhitelist = sr.ReadToEnd();

                    if (!string.IsNullOrWhiteSpace(nationalWhitelist))
                    {
                        allServices.AddRange(nationalWhitelist.Split('|'));
                    }
                }
            }

            allServices.AddRange(gpOutOfHours.Split('|'));

            return string.Join('|', allServices);
        }

        private async Task<CloudBlockBlob> GetBlob(string name)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse(_azureAccountSettings.ConnectionString);

                var client = storageAccount.CreateCloudBlobClient();

                var container = client.GetContainerReference(BlobContainerName);

                await container.CreateIfNotExistsAsync();

                var blob = container.GetBlockBlobReference(name);

                return blob;
            }
            catch (Exception e)
            {
                // TODO: Add application logging

                throw new Exception("", e);
            }
        }
        
        private const string BlobContainerName = "epwhitelist";

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