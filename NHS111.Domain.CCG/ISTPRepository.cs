namespace NHS111.Domain.CCG
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using NHS111.Domain.CCG.Models;

    public interface ISTPRepository
    {
        Task<STPEntity> Get(string ccgId);

        Task<List<STPEntity>> List();
    }
}