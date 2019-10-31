using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FileMapper
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();


                string inputFilePath = ConfigurationManager.AppSettings["InputFilePath"].ToString();
                string mappingFilePath = ConfigurationManager.AppSettings["MappingFilePath"].ToString();



                string fileText = System.IO.File.ReadAllText(""+@""+inputFilePath);

                XDocument xmlDoc = XDocument.Load(""+@""+mappingFilePath);

                var headerList = ConvertXMLToList(xmlDoc);

                var fileModelList = headerList.Where(c => !String.IsNullOrEmpty(c.FileIndex)).Select(c => new FileDataModel
                {
                    Name = c.ColumnName,


                    ColumnIndex = String.IsNullOrEmpty(c.FileIndex) ? -1 : Convert.ToInt32(c.FileIndex) - 1
                }).ToList(); //-1 for array logic


                string formattedText = FormatFileText(fileText);

                string ApiUrl = ConfigurationManager.AppSettings["PostApiUrl"].ToString();
                var dataToPost = GetColumnValuesfromFile(fileModelList, formattedText, ApiUrl);


               
                stopWatch.Stop();
                Console.WriteLine("Data Posted Sucessfully");
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                       ts.Hours, ts.Minutes, ts.Seconds,
                       ts.Milliseconds / 10);
                Console.WriteLine("Time Elapsed:" + elapsedTime);
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Something Went Wrong");
                System.Console.WriteLine(ex.Message);                
                Console.ReadKey();


            }

        }

        private  static List<Mapping> ConvertXMLToList(XDocument xmlDoc) {

            var MappingNodes = from s in xmlDoc.Descendants("Mapping")
                               select new Mapping()
                               {

                                   ColumnName = (string)s.Element("APIColumnName"),
                                   FileIndex = (string)s.Element("FileColumnIndex")
                               };
            return MappingNodes.ToList();
        }

        private static string FormatFileText(string fileContent) {

            StringBuilder sb = new StringBuilder(fileContent);
            sb = sb.Replace(";", ",");
            sb.Replace("\"", string.Empty);
            return sb.ToString();

        }

        private static List<FileDataModel> GetColumnValuesfromFile( List<FileDataModel> filedataModel, string fileText,string Url ) {
            string ApiUrl = Url+"?";
            //HttpClient httpClient = new HttpClient();
            //var content = new StringContent("", Encoding.UTF8, "application/json");


            using (StringReader reader = new StringReader(fileText))
            {

                string line;
                while ((line = reader.ReadLine()) != null)
                {

                    string[] splittedtext = line.Split(','); // split line into comma separated string
                  
                    string QueryString = "";
                    foreach (var listdata in filedataModel)
                    {

                        //if (!String.IsNullOrEmpty(listdata.Values))
                        //{
                        //    listdata.Values = listdata.Values + "," + splittedtext[listdata.ColumnIndex];
                        //}
                        //else
                        //{
                        //    listdata.Values = splittedtext[listdata.ColumnIndex];
                        //}
                        if (!String.IsNullOrEmpty(QueryString))
                        {
                            QueryString = QueryString + "&" + listdata.Name + "=" + splittedtext[listdata.ColumnIndex];
                        }
                        else
                        {
                            QueryString = QueryString + "" + listdata.Name + "=" + splittedtext[listdata.ColumnIndex];

                        }

                    }

                    //var result = httpClient.PostAsync(ApiUrl+QueryString, content).Result;
                SendHttpRequestAsync(ApiUrl+QueryString);
                }

            }

            return filedataModel;
        }

        public  static async void  SendHttpRequestAsync(string URl)
        {
            HttpClient httpClient = new HttpClient();
            var content = new StringContent("", Encoding.UTF8, "application/json");

            Console.WriteLine("Sending Request : "+URl );

            var result = await  httpClient.PostAsync(URl, content);

            Console.WriteLine("RequestUrl:"+ result.RequestMessage.RequestUri.ToString()+ "  Status:"+ result.StatusCode.ToString());
            
        }
    }
}
