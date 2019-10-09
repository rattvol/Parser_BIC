using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Parser_BIC
{
    class Program
    {     

        static void Main(string[] args)
        {
            InitWebServ newService = new InitWebServ();
        }
    }
    public class InitWebServ
    {
        //public Dictionary<double, object> bicBaseArray = new Dictionary<double, object>(); //Сюда загрузим все данные по банкам
        public List<Record> bicBaseArray = new List<Record>(); //Сюда загрузим все данные по банкам
        public string filePath = "C:\\Users\\User\\Downloads\\банк\\result.json"; //сюда сохраняем json файл

        public InitWebServ()
        {
            LoadInitCodeData();
        }
        public void LoadInitCodeData()
        {
            ServiceReference1.CreditOrgInfoSoapClient serviceClass = new ServiceReference1.CreditOrgInfoSoapClient();//экземпляр класса сервиса

            //*************************работа с перечнем внутренних идентификторов*******************
            DataSet ds = serviceClass.EnumBIC();//получение сокращенного списка банков
            var rows = ds.Tables[0].Rows;//создаем объект из строк
            List<double> codes = new List<double>();//создаем перечень для внесения ИД кодов
            for (int i=0; i<rows.Count; i++)
            {
                codes.Add(Convert.ToDouble(rows[i]["IntCode"]));//заполняем перечень кодами
            }
            //******************работа с полными наборами характеристик банков*********************
            int k = 1;
            foreach (double code in codes )//обрабатываем перечень кодов
                {
            DataSet fullDS = serviceClass.CreditInfoByIntCode(code); //по перечню кодов вызываем информацию по каждому банку
            var rowsFull = fullDS.Tables[0].Rows;
                //заполняем внутренние данные организации и сразу пишем в общий перечень
                bicBaseArray.Add(new Record
                {
                    Number = k,
                    OrgName = Convert.ToString(rowsFull[0]["OrgName"]),
                    OrgFullName = Convert.ToString(rowsFull[0]["OrgFullName"]),
                    FactAdr = Convert.ToString(rowsFull[0]["FactAdr"]),
                    BIC = Convert.ToInt32(rowsFull[0]["BIC"]),
                    OrgStatus = Convert.ToString(rowsFull[0]["OrgStatus"])
                });
                k++;
            }
            //******************загоняем в json**********************

            Console.WriteLine("Заполнено {0} банков, k);
            try
            {
                var serializer = new JavaScriptSerializer();
                var jsonBase = serializer.Serialize(bicBaseArray);
                using (StreamWriter streamWrite = new StreamWriter(filePath))
                {
                    streamWrite.Write(jsonBase);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Console.ReadLine();
        }
    }
    public class Record
    {
        public int Number { get; set; }
        public int BIC { get; set; }
        public string OrgName { get; set; }
        public string OrgFullName { get; set; }
        public string FactAdr { get; set; }
        public string OrgStatus { get; set; }

    }

}
