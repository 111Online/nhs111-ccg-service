namespace NHS111.Domain.CCG
{
    using NHS111.Domain.CCG.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISTPRepository
    {
        Task<STPEntity> Get(string ccgId);

        Task<List<STPEntity>> List();
    }
}