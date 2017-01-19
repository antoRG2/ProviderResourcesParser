using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProviderResourcesParser.Models
{
    //Class for details section
    public class DetailsModel
    {
        public MiscellaneousModel miscellaneous { set; get; }
        public LegalStatusModel legalStatus { set; get; }
    }
}