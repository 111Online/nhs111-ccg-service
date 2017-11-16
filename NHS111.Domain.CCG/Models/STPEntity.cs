using System;
using System.Collections.Generic;
using System.Text;

namespace NHS111.Domain.CCG.Models
{
    public class STPEntity
    {
        public string STPId { get; set; }

        public string Name { get; set; }

        public string CCGId { get; set; }

        public string AppName { get; set; }
        public bool Live { get; set; }
    }
}
