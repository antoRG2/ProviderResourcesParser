using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using System.Threading.Tasks;
using System.Web.UI;
using Newtonsoft.Json;
using ProviderResourcesParser.Models;

namespace ProviderResourcesParser.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Parser()
        {
            int pageNumber = 1;
            string pageUrl = "http://tn211.mycommunitypt.com/index.php/component/cpx/?task=search.query&code";

            List<string> mainJsonFile = new List<string>();

            mainJsonFile.Add("{\"menu\": {\"menuitem\": [");

            try
            {
                var web = new HtmlWeb();
                var document = web.Load(pageUrl);
                var page = document.DocumentNode;

                //get last page of the list 
                var paginationList = page.QuerySelectorAll(".pagination a").ToList();
                int lastPage = Int32.Parse(paginationList[paginationList.Count - 2].InnerText);

                while (pageNumber <= lastPage)
                {
                    if (pageNumber != 1)
                    {
                        pageUrl =
                            "http://tn211.mycommunitypt.com/index.php/component/cpx/?task=search.query&view=&page=" +
                            pageNumber.ToString() +
                            "&search_history_id=66775857&unit_list=0&akaSort=0&query=%20&simple_query=";
                        document = web.Load(pageUrl);
                        page = document.DocumentNode;
                    }

                    //check all the element in the providers list
                    Parallel.ForEach(page.QuerySelectorAll(".results li .content a"), item =>
                    //foreach (var item in page.QuerySelectorAll(".results li .content a"))
                    {

                        string jsonFile = "{";

                        for (int tabCuonter = 1; tabCuonter <= 3; tabCuonter++)
                        {
                            var href = item.Attributes.Where(x => x.Name == "href").FirstOrDefault().Value;

                            if (!String.IsNullOrWhiteSpace(href))
                            {
                                //loading page data
                                var itemWeb = new HtmlWeb();
                                string url = "http://tn211.mycommunitypt.com" + href;
                                var itemDocument = itemWeb.Load(url);
                                var itemPage = itemDocument.DocumentNode;

                                if (tabCuonter >= 2)
                                {
                                    url = url.Replace(".view", "");
                                    if (tabCuonter == 2)
                                    {
                                        url = url.Replace(url.Substring(url.IndexOf("search")), "tab=2");
                                    }
                                    else
                                    {
                                        url = url.Replace(url.Substring(url.IndexOf("search")), "tab=3");
                                    }


                                    //loading itemDocument and itemPage with new data from second tab
                                    itemDocument = itemWeb.Load(url);
                                    itemPage = itemDocument.DocumentNode;
                                }
                                else
                                {
                                    jsonFile += "\"Name\":" + "\"" +
                                                (itemPage.QuerySelector("#view_field_name_top") != null
                                                    ? itemPage.QuerySelector("#view_field_name_top")
                                                        .InnerText.Replace("'", "")
                                                    : "") + "\",";


                                    jsonFile += "\"Address\":" + "\"" +
                                                (itemPage.QuerySelector("#view_field_primaryAddressId") != null
                                                    ? itemPage.QuerySelector("#view_field_primaryAddressId")
                                                        .InnerHtml.Replace("<br>", "\u0020")
                                                        .Replace("'", "")
                                                    : "") + "\",";

                                    jsonFile += "\"Telephone\":" + "\"" +
                                                (itemPage.QuerySelector("#view_field_primaryTelephone") != null
                                                    ? itemPage.QuerySelector("#view_field_primaryTelephone")
                                                        .InnerText.Replace("'", "")
                                                    : "") + "\",";

                                    jsonFile += "\"Url\":" + "\"" + (itemPage.QuerySelector("#view_field_url a") != null
                                        ? itemPage.QuerySelector("#view_field_url a")
                                            .Attributes.Where(x => x.Name == "href")
                                            .FirstOrDefault()
                                            .Value.Replace("'", "")
                                        : "") + "\",";
                                }

                                if (tabCuonter == 3)
                                {
                                    var cont = 1;
                                    foreach (var tr in itemPage.QuerySelectorAll("#current_tab tr td"))
                                    {
                                        if (cont % 2 != 0)
                                        {
                                            jsonFile += "\"" +
                                                        (jsonFile.Contains(tr.InnerText)
                                                            ? tr.InnerText.Replace(System.Environment.NewLine, "") +
                                                              cont.ToString()
                                                            : tr.InnerText.Replace(System.Environment.NewLine, ""))
                                                            .Replace("'", "")
                                                            .Replace(";", "")
                                                            .Replace("\n", String.Empty)
                                                            .Replace("\r", String.Empty)
                                                            .Replace("\t", String.Empty)
                                                            .Replace("\"", "") + "\":";
                                        }
                                        else
                                        {
                                            jsonFile += "\"" + tr.InnerText.Replace(System.Environment.NewLine, "")
                                                .Replace("'", "")
                                                .Replace(";", "")
                                                .Replace("\n", String.Empty)
                                                .Replace("\r", String.Empty)
                                                .Replace("\t", String.Empty)
                                                .Replace("\"", "") + "\",";
                                        }

                                        cont++;
                                    }
                                }
                                else
                                {
                                    if (itemPage.QuerySelectorAll("#current_tab p") == null || !itemPage.QuerySelectorAll("#current_tab p").Any())
                                    {
                                        foreach (var pSelector in itemPage.QuerySelectorAll("#current_tab p"))
                                        {
                                            if (pSelector.HasAttributes && pSelector.Attributes["class"] != null)
                                            {
                                                if (pSelector.Attributes["class"].Value.Contains("view_label_type_"))
                                                {

                                                    var itemID = pSelector.Id.Replace("view_label_", "view_");

                                                    if (!jsonFile.Contains(pSelector.InnerHtml))
                                                    {
                                                        var value = itemPage.QuerySelector("#current_tab p#" + itemID);

                                                        jsonFile += "\"" +
                                                                    (pSelector.InnerHtml.Contains("Related Resource")
                                                                        ? "Related Resources"
                                                                        : pSelector.InnerHtml) + "\": " + "\"";


                                                        if (!pSelector.InnerHtml.Contains("Related Resource") &&
                                                            !pSelector.InnerHtml.Contains("Services"))
                                                        {
                                                            jsonFile += !String.IsNullOrEmpty(value.InnerHtml)
                                                                ? value.InnerHtml.Replace("<br>", "")
                                                                    .Replace(System.Environment.NewLine, "")
                                                                    .Replace("'", "")
                                                                    .Replace(";", "")
                                                                    .Replace("\n", String.Empty)
                                                                    .Replace("\r", String.Empty)
                                                                    .Replace("\t", String.Empty)
                                                                    .Replace("\"", "")
                                                                : "";
                                                        }
                                                        else
                                                        {
                                                            if (pSelector.InnerHtml.Contains("Related Resource"))
                                                            {
                                                                foreach (
                                                                    var resource in
                                                                        itemPage.QuerySelectorAll(
                                                                            ".view_type_resource_list a"))
                                                                {
                                                                    if (resource.InnerHtml != "")
                                                                    {
                                                                        jsonFile +=
                                                                            resource.InnerHtml.Replace("\"", "")
                                                                                .Replace("'", "") +
                                                                            " | ";
                                                                    }
                                                                }
                                                                if (jsonFile.EndsWith("|"))
                                                                {
                                                                    jsonFile = jsonFile.Remove(jsonFile.Length - 1);
                                                                }
                                                            }

                                                            if (pSelector.InnerHtml.Contains("Services"))
                                                            {
                                                                string id = pSelector.InnerHtml.Split(' ')[0].ToLower() +
                                                                            pSelector.InnerHtml.Split(' ')[1];
                                                                id = id.Remove(id.Length - 1);

                                                                foreach (
                                                                    var resource in
                                                                        itemPage.QuerySelectorAll("#view_field_" + id + " a")
                                                                    )
                                                                {
                                                                    if (resource.InnerHtml != "" &&
                                                                        !resource.InnerHtml.Contains("Edit "))
                                                                    {
                                                                        if (resource.InnerHtml.Contains("[") &&
                                                                            resource.OuterHtml.IndexOf("code=") > -1)
                                                                        {
                                                                            int pFrom =
                                                                                resource.OuterHtml.IndexOf("code=") +
                                                                                "code= ".Length;
                                                                            int pTo = resource.OuterHtml.LastIndexOf("\" ");
                                                                            String result =
                                                                                resource.OuterHtml.Substring(pFrom,
                                                                                    pTo - pFrom);
                                                                            jsonFile = jsonFile.Insert(jsonFile.Length - 3,
                                                                                "(" + result + ")");
                                                                        }
                                                                        else
                                                                        {
                                                                            jsonFile +=
                                                                                resource.InnerHtml.Replace("\"", "")
                                                                                    .Replace("'", "") +
                                                                                " | ";
                                                                        }
                                                                    }


                                                                }
                                                                if (jsonFile.EndsWith("|"))
                                                                {
                                                                    jsonFile = jsonFile.Remove(jsonFile.Length - 1);
                                                                }
                                                            }
                                                        }

                                                        jsonFile += "\",";
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }


                            }
                        }
                        jsonFile = jsonFile.Remove(jsonFile.Length - 1);
                        jsonFile += "},";
                        mainJsonFile.Add(jsonFile);
                    });

                    pageNumber++;
                }

                //mainJsonFile = mainJsonFile.Remove(mainJsonFile.Length - 1);
                mainJsonFile[mainJsonFile.Count - 1] = mainJsonFile[mainJsonFile.Count - 1].Remove(mainJsonFile[mainJsonFile.Count - 1].Length - 1);
                mainJsonFile.Add("]}}");

                string json = string.Join("", mainJsonFile.ToArray());

                return View("Index", (object)json);
            }
            catch (Exception)
            {

                throw;
            }

        }

    }
}