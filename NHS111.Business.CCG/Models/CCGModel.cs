using Newtonsoft.Json;

namespace NHS111.Business.CCG.Models {

    public class CCGModel {
        public string Postcode { get; set; }
        public string STP { get; set; }
        public string CCG { get; set; }
        public string App { get; set; }
        [JsonProperty(PropertyName = "dosSearchDistance")]
        public string DOSSearchDistance { get; set; }
    }
}