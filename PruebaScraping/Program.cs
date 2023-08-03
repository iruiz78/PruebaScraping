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
using System.Threading;
using System.Xml.Linq;

namespace PruebaScrap
{
    // TODO esta simple y entendible, se puede REFACTORIZAR todo, lo dejamos simple y entendible 
    internal class Program
    {
        static async Task Main(string[] args)
        {

           await ScrapingCuitSelenium();
           await ScrapingSelenium();

           await ScrapingVtex();
        }

        private static async Task ScrapingSelenium()
        {
            // Initialize the Chrome Driver
            using (var driver = new ChromeDriver())
            {
                var products = string.Empty;

                // Go to the home page                 
                driver.Navigate().GoToUrl("https://www.mercadolibre.com.ar/");
                Thread.Sleep(5000);

                // Get the page elements
                var busqueda = driver.FindElement(By.Id("cb1-edit"));
                busqueda.SendKeys("Notebook");

                // Submit
                var buttomSubmit = driver.FindElement(By.ClassName("nav-search-btn"));
                buttomSubmit.Click();

                //// Extract the text and save it into result.txt
                var listOfElements = driver.FindElements(By.ClassName("ui-search-result__content-wrapper"));
                foreach (var element in listOfElements)
                {
                    products += "-----------------------------------------------" + Environment.NewLine;
                    products += element.Text;
                    products += "-----------------------------------------------" + Environment.NewLine;
                }
                driver.GetScreenshot().SaveAsFile($"screen.png", ScreenshotImageFormat.Png);

                File.WriteAllText("result.txt", products);
            }
        }
        private static async Task ScrapingCuitSelenium()
        {
            // Initialize the Chrome Driver
            using (var driver = new ChromeDriver())
            {
                var products = string.Empty;

                // Go to the home page                 
                driver.Navigate().GoToUrl("https://www.anses.gob.ar/consultas/constancia-de-cuil");

                driver.ExecuteScript("document.getElementById('edit-sexo-m').click();"); // Agregar genero a busacar
                // Get the page elements
                var NroDni = driver.FindElement(By.Id("edit-nro-doc"));
                NroDni.SendKeys("");// Agregar nro de dni a busacar

                var NameCuil = driver.FindElement(By.Id("edit-name"));
                NameCuil.SendKeys(""); // Agregar nombre busacar

                var lastNameCuil = driver.FindElement(By.Id("edit-lastname"));
                lastNameCuil.SendKeys(""); // Agregar Apellido a busacar

                var DateCuil = driver.FindElement(By.Id("edit-date"));
                DateCuil.SendKeys(""); // Agregar fecha de nacimientoa busacar

                // Submit
                var buttomSubmit = driver.FindElement(By.Id("edit-submit"));
                buttomSubmit.Click();

                Thread.Sleep(5000);
                driver.GetScreenshot().SaveAsFile($"screen.png", ScreenshotImageFormat.Png);

                // Descargo constancia Cuil
                var descargar = driver.FindElement(By.ClassName("btn-default"));
                descargar.Click();
                Thread.Sleep(5000);
            }
        }

        #region VTEX
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
            ProcessFile(Data, "ProductosVtex.csv");
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

        private static void ProcessFile(List<string> data, string nameFile)
        {
            var path = nameFile;
            MemoryStream stream = new MemoryStream();
            if (!File.Exists(path))
            {
                var file = File.CreateText(path);
                file.Close();
            }
            byte[] buffer = File.ReadAllBytes(path);
            byte[] newData = System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, data));
            stream.Write(newData, 0, newData.Length);
            stream.Write(buffer, 0, buffer.Length);
            File.WriteAllBytes(path, stream.GetBuffer());
            stream.Dispose();
        }
        #endregion

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
