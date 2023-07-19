using System;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;
using PuppeteerSharp;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Drawing.Imaging;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Linq;

namespace PruebaScrap
{
    // TODO esta simple y entendible, se puede REFACTORIZAR todo, lo dejamos simple y entendible 
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //GetPruebaSelenium();

           await ScrapingVtex();
        }

        public static async Task ScrapingVtex()
        {
            var Categories = await GetCategories();
            var products = new List<Product>();
            foreach (var category in Categories)
            {
                var response = await CallUrl($"https://www.hiperlibertad.com.ar/api/catalog_system/pub/products/search/{ category.description}");
                dynamic productsResponse = JsonConvert.DeserializeObject<dynamic>(response);
                foreach (var item in productsResponse)
                {
                    Product product =new Product();
                    product.id= item.productId;
                    product.productName = item.productName;
                    products.Add(product);
                }
            }
            List<string> Data = products.Select(n => $" {n.id}  {n.productName}").ToList();

            var path = "ProductosVtex.csv";
            MemoryStream stream = new MemoryStream();
            if (!File.Exists(path))
            {
                var file = File.CreateText(path);
                file.Close();
            }
            byte[] buffer = File.ReadAllBytes(path);
            byte[] newData = System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, Data));
            stream.Write(newData, 0, newData.Length);
            stream.Write(buffer, 0, buffer.Length);
            File.WriteAllBytes(path, stream.GetBuffer());
            stream.Dispose();
        }

        private static async Task<List<Category>> GetCategories()
        {

            var response = await CallUrl("https://www.easy.com.ar/api/catalog_system/pub/category/tree/1");
            var categories = new List<Category>();
            dynamic categoriesResponse = JsonConvert.DeserializeObject<dynamic>(response);
            foreach (var item in categoriesResponse)
            {
                Category c = new Category();
                c.id=item.id;
                c.description = item.name;
                categories.Add(c);
            }
            return categories;
        }

        private static async Task<string> CallUrl(string fullUrl)
        {
            try
            {
                HttpClient client = new HttpClient();
                var response = await client.GetAsync(fullUrl);
                return await response.Content.ReadAsStringAsync();
            }catch(Exception ex)
            {
                return null;
            }
        }

        private static async void GetPruebaSelenium()
        {
            // Initialize the Chrome Driver
            using (var driver = new ChromeDriver())
            {
                var h3 = string.Empty;
                for (int i = 65466; i < 65490; i++)
                {
                    // Go to the home page
                    driver.Navigate().GoToUrl("https://www.ssn.gob.ar/storage/registros/productores/productoresactivosfiltro.asp");

                    driver.ExecuteScript("document.getElementsByName('form1')[0].target=''");

                    // Get the page elements
                    var NroMatricula = driver.FindElement(By.Id("matricula"));
                    NroMatricula.SendKeys(i.ToString());

                    var FromSubmit = driver.FindElement(By.Name("form1"));
                    FromSubmit.TagName.Replace("target=\"_blank\"", "");

                    var SubmitButton = driver.FindElement(By.Name("Submit"));
                    SubmitButton.Submit();

                    //// Extract the text and save it into result.txt
                    var listOfElements = driver.FindElements(By.XPath("//h3"));
                    foreach (var element in listOfElements)
                    {
                        h3 += "|" + element.Text;
                    }
                    listOfElements = driver.FindElements(By.XPath("//h5"));
                    foreach (var element in listOfElements)
                    {
                        h3 += "|" + element.Text;
                    }
                    h3 += System.Environment.NewLine;
                    driver.GetScreenshot().SaveAsFile($"screen{i}.png", ScreenshotImageFormat.Png);
                }
                File.WriteAllText("result.txt", h3);
            }
        }
    }


    struct Category{
        public string id;
        public string description;
    }

    struct Product
    {
        public string id;
        public string productName;
    }

}
