using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using CsvHelper;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Newtonsoft.Json;

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
                    //foreach (var item in page.QuerySelectorAll(".results li .content a"))
                    //{



                    Parallel.ForEach(page.QuerySelectorAll(".results li .content a"), item =>
                    {
                        string jsonFile = "[{";

                        for (int tabCounter = 1; tabCounter <= 3; tabCounter++)
                        {
                            var href = item.Attributes.Where(x => x.Name == "href").FirstOrDefault().Value;

                            if (!String.IsNullOrWhiteSpace(href))
                            {
                                //loading page data
                                var itemWeb = new HtmlWeb();
                                string url = "http://tn211.mycommunitypt.com" + href;
                                var itemDocument = itemWeb.Load(url);
                                var itemPage = itemDocument.DocumentNode;
                                var completeAddress = "";

                                if (tabCounter >= 2)
                                {
                                    url = url.Replace(".view", "");
                                    if (tabCounter == 2)
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
                                    jsonFile += "\"Page Number\":" + "\"" + pageNumber + "\",";

                                    jsonFile += "\"Name\":" + "\"" +
                                                        (itemPage.QuerySelector("#view_field_name_top") != null
                                                            ? itemPage.QuerySelector("#view_field_name_top")
                                                                .InnerText.Replace("'", "")
                                                                    .Replace(";", "")
                                                                    .Replace("\n", String.Empty)
                                                                    .Replace("\r", String.Empty)
                                                                    .Replace("\t", String.Empty)
                                                                    .Replace(@"\", " ")
                                                                    .Replace("\"", "")
                                                            : "") + "\",";

                                    completeAddress = (itemPage.QuerySelector("#view_field_primaryAddressId") != null
                                            ? itemPage.QuerySelector("#view_field_primaryAddressId")
                                                .InnerHtml.Replace("<br>", "\u0020")
                                                .Replace("'", "")
                                                .Replace(";", "")
                                                .Replace("\n", String.Empty)
                                                .Replace("\r", String.Empty)
                                                .Replace("\t", String.Empty)
                                                .Replace(@"\", " ")
                                                .Replace("\"", "")
                                            : "");


                                    if (completeAddress != "")
                                    {
                                        //Check if address has more than one ',' and replace that with an empty space to just let one comma
                                        if (completeAddress.Count(x => x == ',') > 1)
                                        {
                                            var regex = new Regex(Regex.Escape(","));
                                            completeAddress = regex.Replace(completeAddress, "", completeAddress.Count(x => x == ',') - 1);
                                        }

                                        if (completeAddress.Count(x => x == ',') >= 1)
                                        {
                                            jsonFile += "\"Address\":" + "\"" +
                                                        completeAddress.Split(',')[0] + "\",";

                                            jsonFile += "\"State\":" + "\"" +
                                                        completeAddress.Split(',')[1].Trim().Split(' ')[0] + "\",";

                                            jsonFile += "\"Zip\":" + "\"" +
                                                        completeAddress.Split(',')[1].Trim().Split(' ')[2] + "\",";
                                        }
                                        else
                                        {
                                            jsonFile += "\"Address\":" + "\"" +
                                                          completeAddress + "\",";
                                        }

                                    }


                                    jsonFile += "\"Telephone\":" + "\"" +
                                                        (itemPage.QuerySelector("#view_field_primaryTelephone") != null
                                                            ? itemPage.QuerySelector("#view_field_primaryTelephone")
                                                                .InnerText.Replace("'", "")
                                                                    .Replace(";", "")
                                                                    .Replace("\n", String.Empty)
                                                                    .Replace("\r", String.Empty)
                                                                    .Replace("\t", String.Empty)
                                                                    .Replace(@"\", " ")
                                                                    .Replace("\"", "")
                                                            : "") + "\",";

                                    jsonFile += "\"Url\":" + "\"" + (itemPage.QuerySelector("#view_field_url a") != null
                                                ? itemPage.QuerySelector("#view_field_url a")
                                                    .Attributes.Where(x => x.Name == "href")
                                                    .FirstOrDefault()
                                                    .Value.Replace("'", "")
                                                                    .Replace(";", "")
                                                                    .Replace("\n", String.Empty)
                                                                    .Replace("\r", String.Empty)
                                                                    .Replace("\t", String.Empty)
                                                                    .Replace(@"\", " ")
                                                                    .Replace("\"", "")
                                                : "") + "\",";
                                }

                                if (tabCounter == 3)
                                {
                                    var cont = 0;
                                    var labels = itemPage.QuerySelectorAll("#current_tab .view_type_label");

                                    foreach (var table in itemPage.QuerySelectorAll("#current_tab table"))
                                    {
                                        jsonFile += "\"" + labels.ElementAt(cont).InnerHtml + "\":" + "\"";

                                        foreach (var tr in table.QuerySelectorAll("tr td"))
                                        {
                                            if (!tr.Attributes[0].Value.Contains("width: 125px; padding-bottom: 10px; vertical-align: top; font-weight: bold;"))
                                            {
                                                jsonFile += tr.InnerText.Replace(Environment.NewLine, "")
                                                           .Replace("'", "")
                                                           .Replace(";", "")
                                                           .Replace("\n", String.Empty)
                                                           .Replace("\r", String.Empty)
                                                           .Replace("\t", String.Empty)
                                                           .Replace(@"\", " ")
                                                           .Replace("\"", "") + "| ";
                                            }
                                        }
                                        jsonFile += "\",";
                                        cont++;
                                    }


                                }
                                else
                                {
                                    if (itemPage.QuerySelectorAll("#current_tab p").Any())
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
                                                            jsonFile += (value != null && !String.IsNullOrEmpty(value.InnerHtml))
                                                                        ? value.InnerHtml.Replace("<br>", "")
                                                                            .Replace(Environment.NewLine, "")
                                                                            .Replace("'", "")
                                                                            .Replace(";", "")
                                                                            .Replace("\n", String.Empty)
                                                                            .Replace("\r", String.Empty)
                                                                            .Replace("\t", String.Empty)
                                                                            .Replace(@"\", " ")
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
                                                                                        .Replace("'", "").Replace(@"\", " ") +
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
                                                                                            .Replace("'", "").Replace(@"\", " ") +
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
                        jsonFile += "}]";

                        jsonFile = jsonFile.Replace(@"\", " ");

                        mainJsonFile.Add(jsonFile);
                    });

                    pageNumber++;
                }

                string csv = jsonToCSV(mainJsonFile, ",");

                string path = @"c:\Provider\ProviderResourcesFile.csv";

                System.IO.File.WriteAllText(path, csv);

                return File(new System.Text.UTF8Encoding().GetBytes(csv),
                    "text/csv", "Provider Resources File.csv");

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static DataTable jsonStringToTable(List<string> jsonContent)
        {
            DataTable mainTable = new DataTable();
            foreach (var json in jsonContent)
            {
                DataTable dt = JsonConvert.DeserializeObject<DataTable>(json);
                mainTable.Merge(dt);
            }


            return mainTable;
        }

        public static string jsonToCSV(List<string> jsonContent, string delimiter)
        {
            StringWriter csvString = new StringWriter();
            using (var csv = new CsvWriter(csvString))
            {
                csv.Configuration.SkipEmptyRecords = true;
                csv.Configuration.WillThrowOnMissingField = false;
                csv.Configuration.Delimiter = delimiter;

                using (var dt = jsonStringToTable(jsonContent))
                {
                    foreach (DataColumn column in dt.Columns)
                    {
                        csv.WriteField(column.ColumnName);
                    }
                    csv.NextRecord();

                    foreach (DataRow row in dt.Rows)
                    {
                        for (var i = 0; i < dt.Columns.Count; i++)
                        {
                            csv.WriteField(row[i]);
                        }
                        csv.NextRecord();
                    }
                }
            }
            return csvString.ToString();
        }
    }
}