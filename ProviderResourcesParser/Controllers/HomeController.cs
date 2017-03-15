using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using System.Threading.Tasks;
using ProviderResourcesParser.Models;

namespace ProviderResourcesParser.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Start()
        {
            #region

            int pageNumber = 1;
            string pageUrl = "http://tn211.mycommunitypt.com/index.php/component/cpx/?task=search.query&code";

            //list of all the resources found
            List<ResourceModel> resourceList = new List<ResourceModel>();
            try
            {
                //load first page
                var web = new HtmlWeb();
                var document = web.Load(pageUrl);
                var page = document.DocumentNode;

                //get last page of the list 
                var paginationList = page.QuerySelectorAll(".pagination a").ToList();
                int lastPage = 2;//Int32.Parse(paginationList[paginationList.Count - 2].InnerText);

                while (pageNumber <= lastPage)
                {
                    if (pageNumber != 1)
                    {
                        pageUrl = "http://tn211.mycommunitypt.com/index.php/component/cpx/?task=search.query&view=&page=" +
                                  pageNumber.ToString() +
                                  "&search_history_id=66775857&unit_list=0&akaSort=0&query=%20&simple_query=";
                        document = web.Load(pageUrl);
                        page = document.DocumentNode;
                    }
                    //check all the element in the providers list
                    Parallel.ForEach(page.QuerySelectorAll(".results li .content a"), item =>
                    {
                        ResourceModel resource = new ResourceModel();

                        var href = item.Attributes.Where(x => x.Name == "href").FirstOrDefault().Value;

                        if (!String.IsNullOrWhiteSpace(href))
                        {
                            //loading page data
                            var itemWeb = new HtmlWeb();
                            string url = "http://tn211.mycommunitypt.com" + href;
                            var itemDocument = itemWeb.Load(url);
                            var itemPage = itemDocument.DocumentNode;

                            //loading first tab Main information/Overview

                            //extracting info from page to objects
                            resource.resourceName = itemPage.QuerySelector("#view_field_name_top") != null
                                    ? itemPage.QuerySelector("#view_field_name_top").InnerText
                                    : "";
                            resource.address = itemPage.QuerySelector("#view_field_primaryAddressId") != null
                                ? itemPage.QuerySelector("#view_field_primaryAddressId").InnerHtml.Replace("<br>", "\u0020")
                                : "";
                            resource.phone = itemPage.QuerySelector("#view_field_primaryTelephone") != null
                                ? itemPage.QuerySelector("#view_field_primaryTelephone").InnerText
                                : "";
                            resource.url = itemPage.QuerySelector("#view_field_url a") != null
                                ? itemPage.QuerySelector("#view_field_url a")
                                    .Attributes.Where(x => x.Name == "href")
                                    .FirstOrDefault()
                                    .Value
                                : "";

                            //Adding overview object info
                            resource.overview = new OverviewModel();
                            resource.overview.description = itemPage.QuerySelector("#view_field_description") != null
                                ? itemPage.QuerySelector("#view_field_description")
                                    .InnerHtml.Replace("\n", "")
                                    .Replace("<br>", "")
                                : "";
                            resource.overview.primaryServices = new List<string>();
                            foreach (var primaryService in itemPage.QuerySelectorAll("#view_field_primaryServices"))
                            {
                                if (primaryService.QuerySelector("a") != null)
                                {
                                    String result = "";
                                    int pFrom, pTo = 0;
                                    if (primaryService.InnerHtml != "")
                                    {
                                        if (primaryService.InnerHtml.Contains("[") &&
                                            primaryService.OuterHtml.IndexOf("code=") > -1)
                                        {
                                            pFrom =
                                               primaryService.OuterHtml.IndexOf("code=") +
                                               "code= ".Length;
                                            pTo = primaryService.OuterHtml.IndexOf("\" style=");
                                            if (pFrom > 0 && pTo > 0)
                                            {
                                                result = " (" + primaryService.OuterHtml.Substring(pFrom,
                                                        pTo - pFrom) + ") ";
                                            }
                                        }
                                    }
                                    resource.overview.primaryServices.Add(primaryService.QuerySelector("a").InnerText + result);
                                }
                            }

                            //Adding general information object info
                            resource.overview.generalInformation = new GeneralInformationModel();
                            resource.overview.generalInformation.hours = itemPage.QuerySelector("#view_field_hours") != null
                                ? itemPage.QuerySelector("#view_field_hours").InnerText
                                : "";
                            resource.overview.generalInformation.intakeProcess =
                                itemPage.QuerySelector("#view_field_intakeProcedure") != null
                                    ? itemPage.QuerySelector("#view_field_intakeProcedure").InnerText
                                    : "";
                            resource.overview.generalInformation.programFees =
                                itemPage.QuerySelector("#view_field_programFees") != null
                                    ? itemPage.QuerySelector("#view_field_programFees").InnerText
                                    : "";
                            resource.overview.generalInformation.eligibility =
                                itemPage.QuerySelector("#view_field_eligibility") != null
                                    ? itemPage.QuerySelector("#view_field_eligibility").InnerText
                                    : "";
                            resource.overview.generalInformation.handicapAccessible =
                                itemPage.QuerySelector("#view_field_accessibility_flag") != null
                                    ? itemPage.QuerySelector("#view_field_accessibility_flag").InnerText
                                    : "";
                            resource.overview.generalInformation.isShelter =
                                itemPage.QuerySelector("#view_field_is_shelter") != null
                                    ? itemPage.QuerySelector("#view_field_is_shelter").InnerText
                                    : "";
                            resource.overview.generalInformation.relatedResources = new List<string>();

                            //Parallel.ForEach(itemPage.QuerySelectorAll("#view_field_providerChildren a"), relatedResource =>
                            foreach (var relatedResource in itemPage.QuerySelectorAll("#view_field_providerChildren a"))
                            {
                                if (relatedResource != null)
                                {
                                    resource.overview.generalInformation.relatedResources.Add(relatedResource.InnerText);
                                }
                            }
                            //loading second tab Details

                            //modifying URL to load second tab
                            url = url.Replace(".view", "");
                            url = url.Replace(url.Substring(url.IndexOf("search")), "tab=2");

                            //loading itemDocument and itemPage with new data from second tab
                            itemDocument = itemWeb.Load(url);
                            itemPage = itemDocument.DocumentNode;

                            //Adding details object info
                            resource.details = new DetailsModel();

                            //Adding miscellaneous object info
                            resource.details.miscellaneous = new MiscellaneousModel();
                            resource.details.miscellaneous.dateOfOfficialChange =
                                itemPage.QuerySelector("#view_field_dateOfficialchange") != null
                                    ? itemPage.QuerySelector("#view_field_dateOfficialchange").InnerText
                                    : "";
                            resource.details.miscellaneous.aka = itemPage.QuerySelector("#view_field_aka") != null
                                ? itemPage.QuerySelector("#view_field_aka").InnerText
                                : "";
                            resource.details.miscellaneous.Wishlist = itemPage.QuerySelector("#view_field_wishlist") != null
                                ? itemPage.QuerySelector("#view_field_wishlist").InnerText
                                : "";
                            resource.details.miscellaneous.volunteerOpportunities =
                                itemPage.QuerySelector("#view_field_volunteer") != null
                                    ? itemPage.QuerySelector("#view_field_volunteer").InnerText
                                    : "";
                            resource.details.miscellaneous.relatedResource =
                                itemPage.QuerySelector("#view_field_providerParent") != null
                                    ? itemPage.QuerySelector("#view_field_providerParent").InnerText
                                    : "";

                            //Adding legal status object info
                            resource.details.legalStatus = new LegalStatusModel();
                            resource.details.legalStatus.yearIncorporated =
                                itemPage.QuerySelector("#view_field_year_incorporated") != null
                                    ? itemPage.QuerySelector("#view_field_year_incorporated").InnerText
                                    : "";
                            resource.details.legalStatus.primaryServices = new List<string>();
                            foreach (var primaryService in itemPage.QuerySelectorAll("#view_field_primaryServices"))
                            {
                                if (primaryService.QuerySelector("a") != null)
                                {
                                    resource.details.legalStatus.primaryServices.Add(
                                        primaryService.QuerySelector("a").InnerText);
                                }
                            }
                            resource.details.legalStatus.secondaryServices = new List<string>();
                            foreach (var secondaryService in itemPage.QuerySelectorAll("#view_field_secondaryServices"))
                            {
                                if (secondaryService.QuerySelector("a") != null)
                                {
                                    resource.details.legalStatus.secondaryServices.Add(
                                        secondaryService.QuerySelector("a").InnerText);
                                }
                            }
                            resource.details.legalStatus.relatedResources = new List<string>();
                            foreach (var relatedResource in itemPage.QuerySelectorAll("#view_field_providerChildren a"))
                            {
                                if (relatedResource != null)
                                {
                                    resource.details.legalStatus.relatedResources.Add(relatedResource.InnerText);
                                }
                            }
                            //loading third tab Details

                            //modifying URL to load third tab
                            url = url.Replace("tab=2", "tab=3");

                            //loading itemDocument and itemPage with new data from third tab
                            itemDocument = itemWeb.Load(url);
                            itemPage = itemDocument.DocumentNode;

                            //Adding contacts object info
                            resource.contacts = new ContactModel();
                            resource.contacts.addressListings = new List<Tuple<string, string>>();
                            if (itemPage.QuerySelector("#view_field_addressesLabel + div table") != null)
                            {
                                foreach (
                                    var addressListing in
                                        itemPage.QuerySelectorAll("#view_field_addressesLabel + div table tr"))
                                {
                                    string addressDetails = addressListing.QuerySelectorAll("td")
                                        .ToList()[1].InnerHtml.Replace("<br>", "  ");
                                    ;
                                    resource.contacts.addressListings.Add(
                                        new Tuple<string, string>(
                                            addressListing.QuerySelectorAll("td").ToList()[0].InnerText,
                                            addressDetails));
                                }
                            }
                            resource.contacts.contacts = new List<Tuple<string, string>>();
                            if (itemPage.QuerySelector("#view_field_contactsLabel + div table") != null)
                            {
                                foreach (
                                    var contact in itemPage.QuerySelectorAll("#view_field_contactsLabel + div table tr"))
                                {
                                    var tds = contact.QuerySelectorAll("td").ToList();
                                    string contactDetails = "";
                                    foreach (var p in tds[1].QuerySelectorAll("p").ToList())
                                    {
                                        contactDetails += p.InnerText + "\u0020\u0020";
                                    }

                                    resource.contacts.contacts.Add(
                                        new Tuple<string, string>(contact.QuerySelectorAll("td").ToList()[0].InnerText,
                                            contactDetails));
                                }
                            }
                            resource.contacts.phoneNumbers = new List<Tuple<string, string>>();
                            if (itemPage.QuerySelector("#view_field_phonesLabel + div table") != null)
                            {
                                foreach (var phone in itemPage.QuerySelectorAll("#view_field_phonesLabel + div table tr"))
                                {
                                    resource.contacts.phoneNumbers.Add(
                                        new Tuple<string, string>(phone.QuerySelectorAll("td").ToList()[0].InnerText,
                                            phone.QuerySelectorAll("td").ToList()[1].InnerText));
                                }
                            }
                        }
                        //end of page reading

                        resourceList.Add(resource);
                    });
                    //increasing page number to read next page
                    pageNumber++;
                }
                //end of pagination read

                #endregion

                #region
                //Start file creation
                StringBuilder sb = new StringBuilder();

                string header = "Name,Address,Phone,Url,Description,Primary Services,Hours,Intake Process," +
                                "Program Fees,Eligibility,Handicap Accessible?(Yes=1; No=0),Is Shelter?(Yes=1; No=0),Related Resources,AKA,Date of Official Change," +
                                "Volunteer Opportunities,Wishlist,Related Resource,Year Incorporated,Primary Services," +
                                "Secondary Services,Related Resources,Address Listings,Contacts,Phone Numbers";
                string lineBody = "";
                string empty = "No Information Available,";
                string separatorReplacer = ";";

                sb.AppendLine(header);

                foreach (var resource in resourceList)
                {
                    lineBody += (resource.resourceName.ToUpper().Replace(",", separatorReplacer)) + ",";
                    lineBody += resource.address != "" ? (resource.address).Replace(",", separatorReplacer) + "," : empty;
                    lineBody += resource.phone != "" ? (resource.phone).Replace(",", separatorReplacer) + "," : empty;
                    lineBody += resource.url != "" ? (resource.url).Replace(",", separatorReplacer) + "," : empty;

                    lineBody += resource.overview.description != ""
                        ? (resource.overview.description).Replace(",", separatorReplacer) + ","
                        : empty;

                    if (resource.overview.primaryServices.Count != 0)
                    {
                        foreach (var ps in resource.overview.primaryServices)
                        {
                            lineBody += ps != "" ? (ps.Replace(",", separatorReplacer)) + ".\u0020" : empty;
                        }
                        lineBody += ",";
                    }
                    else
                    {
                        lineBody += empty;
                    }

                    lineBody += resource.overview.generalInformation.hours != ""
                        ? (resource.overview.generalInformation.hours.Replace(",", separatorReplacer)) + ","
                        : empty;

                    lineBody += resource.overview.generalInformation.intakeProcess != ""
                        ? (resource.overview.generalInformation.intakeProcess.Replace(",", separatorReplacer)) + ","
                        : empty;

                    lineBody += resource.overview.generalInformation.programFees != ""
                        ? (resource.overview.generalInformation.programFees.Replace(",", separatorReplacer)) + ","
                        : empty;

                    lineBody += resource.overview.generalInformation.eligibility != ""
                        ? (resource.overview.generalInformation.eligibility.Replace(",", separatorReplacer)) + ","
                        : empty;

                    if (resource.overview.generalInformation.handicapAccessible != "")
                    {
                        if (resource.overview.generalInformation.handicapAccessible == "Yes")
                        {
                            lineBody += "1,";
                        }
                        else if (resource.overview.generalInformation.handicapAccessible == "No")
                        {
                            lineBody += "0,";
                        }
                        else
                        {
                            lineBody += empty;
                        }
                    }
                    else
                    {
                        lineBody += empty;
                    }
                    if (resource.overview.generalInformation.isShelter != "")
                    {
                        if (resource.overview.generalInformation.isShelter == "Yes")
                        {
                            lineBody += "1,";
                        }
                        else if (resource.overview.generalInformation.isShelter == "No")
                        {
                            lineBody += "0,";
                        }
                        else
                        {
                            lineBody += empty;
                        }
                    }
                    else
                    {
                        lineBody += empty;
                    }

                    if (resource.overview.generalInformation.relatedResources.Count != 0)
                    {
                        foreach (var rr in resource.overview.generalInformation.relatedResources)
                        {
                            lineBody += rr != ""
                                ? (rr.Replace(",", separatorReplacer)) + ".\u0020"
                                : empty;
                        }
                        lineBody += ",";
                    }
                    else
                    {
                        lineBody += empty;
                    }

                    lineBody += resource.details.miscellaneous.aka != ""
                        ? (resource.details.miscellaneous.aka.Replace(",", separatorReplacer)) + ","
                        : empty;

                    lineBody += resource.details.miscellaneous.dateOfOfficialChange != ""
                        ? (resource.details.miscellaneous.dateOfOfficialChange.Replace(",", separatorReplacer)) + ","
                        : empty;

                    lineBody += resource.details.miscellaneous.volunteerOpportunities != ""
                        ? (resource.details.miscellaneous.volunteerOpportunities.Replace(",", separatorReplacer)) + ","
                        : empty;

                    lineBody += resource.details.miscellaneous.Wishlist != ""
                        ? (resource.details.miscellaneous.Wishlist.Replace(",", separatorReplacer)) + ","
                        : empty;

                    lineBody += resource.details.miscellaneous.relatedResource != ""
                        ? (resource.details.miscellaneous.relatedResource.Replace(",", separatorReplacer)) + ","
                        : empty;

                    lineBody += resource.details.legalStatus.yearIncorporated != ""
                        ? (resource.details.legalStatus.yearIncorporated.Replace(",", separatorReplacer)) + ","
                        : empty;

                    if (resource.details.legalStatus.primaryServices.Count != 0)
                    {
                        foreach (var ps in resource.details.legalStatus.primaryServices)
                        {
                            lineBody += ps != ""
                                ? (ps.Replace(",", separatorReplacer)) + ".\u0020"
                                : empty;
                        }
                        lineBody += ",";
                    }
                    else
                    {
                        lineBody += empty;
                    }

                    if (resource.details.legalStatus.secondaryServices.Count != 0)
                    {
                        foreach (var ss in resource.details.legalStatus.secondaryServices)
                        {
                            lineBody += ss != ""
                                ? (ss.Replace(",", separatorReplacer)) + ".\u0020"
                                : empty;
                        }
                        lineBody += ",";
                    }
                    else
                    {
                        lineBody += empty;
                    }

                    if (resource.details.legalStatus.relatedResources.Count != 0)
                    {
                        foreach (var rrs in resource.details.legalStatus.relatedResources)
                        {
                            lineBody += rrs != ""
                                ? (rrs.Replace(",", separatorReplacer)) + ".\u0020"
                                : empty;
                        }
                        lineBody += ",";
                    }
                    else
                    {
                        lineBody += empty;
                    }

                    if (resource.contacts.addressListings.Count != 0)
                    {
                        foreach (var al in resource.contacts.addressListings)
                        {
                            lineBody += (al.Item1 + ": " + al.Item2.Replace(",", separatorReplacer)) + ".\u0020";
                        }
                        lineBody += ",";
                    }
                    else
                    {
                        lineBody += empty;
                    }

                    if (resource.contacts.contacts.Count != 0)
                    {
                        foreach (var con in resource.contacts.contacts)
                        {
                            lineBody += (con.Item1 + ": " + con.Item2.Replace(",", separatorReplacer)) + ".\u0020";
                        }
                        lineBody += ",";
                    }
                    else
                    {
                        lineBody += empty;
                    }

                    if (resource.contacts.phoneNumbers.Count != 0)
                    {
                        foreach (var pn in resource.contacts.phoneNumbers)
                        {
                            lineBody += (pn.Item1 + ": " + pn.Item2.Replace(",", separatorReplacer)) + ".\u0020";
                        }
                        lineBody += ",";
                    }
                    else
                    {
                        lineBody += empty;
                    }

                    sb.AppendLine(lineBody);
                    lineBody = "";
                }
                //end file creation

                #endregion

                return File(new System.Text.UTF8Encoding().GetBytes(sb.ToString()),
                    "text/csv", "Provider Resources File.csv");
            }

            catch (Exception e)
            {
                return View("Error");
            }
        }
    }
}
