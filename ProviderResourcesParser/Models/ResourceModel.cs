using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProviderResourcesParser.Models
{
    //Class for resource section
    public class ResourceModel
    {
        public string resourceName { set; get; }
        public string address{set; get;}
        public string phone { set; get; }
        public string url { set; get; }
        public OverviewModel overview { set; get; }
        public DetailsModel details { set; get; }
        public ContactModel contacts { set; get; }
    }
}