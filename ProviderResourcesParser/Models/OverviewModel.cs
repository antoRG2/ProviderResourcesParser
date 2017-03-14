using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProviderResourcesParser.Models
{
    //Class for overview section
    public class OverviewModel
    {
        public string description { set; get; }
        public List<string> primaryServices { set; get; }
        public GeneralInformationModel generalInformation { set; get; }
    }
}