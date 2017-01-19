using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProviderResourcesParser.Models
{
    //Class for contact section
    public class ContactModel
    {
        public List<Tuple<string, string>> addressListings { set; get; }
        public List<Tuple<string, string>> contacts { set; get; }
        public List<Tuple<string, string>> phoneNumbers { set; get; }
    }
}