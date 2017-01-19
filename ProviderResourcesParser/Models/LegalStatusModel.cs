using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProviderResourcesParser.Models
{
    //Class for legal status section
    public class LegalStatusModel
    {
        public string yearIncorporated { set; get; }
        public List<string> primaryServices { set; get; }
        public List<string> secondaryServices { set; get; }
        public List<string> relatedResources { set; get; }
    }
}