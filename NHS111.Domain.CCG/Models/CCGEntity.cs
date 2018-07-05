
namespace NHS111.Domain.CCG.Models {
    using Microsoft.WindowsAzure.Storage.Table;

    public class CCGEntity
        : TableEntity {
        public string Postcode { get; set; }
        public string CCG { get; set; }
        public string CCGId { get; set; }
        public string App { get; set; }

    }
}