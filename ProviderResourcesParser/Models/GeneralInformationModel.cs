using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProviderResourcesParser.Models
{
    //Class for general information section
    public class GeneralInformationModel
    {
        public string hours { set; get; }
        public string intakeProcess { set; get; }
        public string programFees { set; get; }
        public string eligibility { set; get; }
        public string handicapAccessible { set; get; }
        public string isShelter { set; get; }
        public List<string> relatedResources { set; get; }
    }
}