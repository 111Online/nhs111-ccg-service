﻿using System.Collections.Generic;
using System.Threading.Tasks;
using NHS111.Business.CCG.Models;

namespace NHS111.Business.CCG.Services
{
    public interface ICCGService
    {
        Task<CCGModel> GetCCGDetails(string postcode);

        Task<CCGDetailsModel> GetDetails(string postcode);

        Task<List<CCGSummaryModel>> List();
    }
}