using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Web.Script.Serialization;

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
        public List<Record> bicBaseArray = new List<Record>(); //Сюда загрузим все данные по банкам
        string filePath =  Environment.GetEnvironmentVariable("USERPROFILE") + Properties.Settings.Default.FilePath;//путь к файлу

        public InitWebServ()
        {
            LoadInitCodeData();
        }
        public void LoadInitCodeData()
        {
            ServiceReference1.CreditOrgInfoSoapClient serviceClass = new ServiceReference1.CreditOrgInfoSoapClient();//экземпляр класса сервиса

            //вызываем заполнение справочника регионов
            RegionBase rb = new RegionBase(serviceClass);

            //*************************работа с перечнем внутренних идентификторов*******************
            DataSet ds = serviceClass.EnumBIC();//получение сокращенного списка банков
            var rows = ds.Tables[0].Rows;//создаем объект из строк
            List<double> codes = new List<double>();//создаем перечень для внесения ИД кодов
            for (int i = 0; i < rows.Count; i++)
            {
                codes.Add(Convert.ToDouble(rows[i]["IntCode"]));//заполняем перечень кодами
            }
            //******************работа с полными наборами характеристик банков*********************
            int k = 1;
            RenameOrg renameObj = new RenameOrg();
            Console.WriteLine();
            Console.Write("Заполнение данных по банкам");
            foreach (double code in codes)//обрабатываем перечень кодов
            {
                DataSet fullDS = serviceClass.CreditInfoByIntCode(code); //по перечню кодов вызываем информацию по каждому банку
                var rowsFull = fullDS.Tables[0].Rows;
                //заполняем внутренние данные организации и сразу пишем в общий перечень
                bicBaseArray.Add(new Record
                {
                    Number = k,        
                    BIC = Convert.ToInt32(rowsFull[0]["BIC"]),
                    items = new Character
                    {
                        OrgName = Convert.ToString(rowsFull[0]["OrgName"]),
                        OrgMiddleName = renameObj.RenameThis(Convert.ToString(rowsFull[0]["OrgName"]), Convert.ToString(rowsFull[0]["OrgFullName"])),
                        OrgFullName = Convert.ToString(rowsFull[0]["OrgFullName"]),
                        FactAdr = Convert.ToString(rowsFull[0]["FactAdr"]),
                        OrgStatus = Convert.ToString(rowsFull[0]["OrgStatus"]),
                        CNAME = rb.FindRegion(Convert.ToDecimal(rowsFull[0]["RegCode"])),
                        Phones = Convert.ToString(rowsFull[0]["phones"])
                    }
                });
                k++;
                if (k%20==0) Console.Write("."); 
            }
            Console.WriteLine();
            //******************загоняем в json**********************

            Console.WriteLine("Заполнено {0} банков", k);
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
        public Character items { get; set; }
    }
    public class Character
    {
        public string OrgName { get; set; }
        public string OrgMiddleName { get; set; }
        public string OrgFullName { get; set; }
        public string FactAdr { get; set; }
        public string OrgStatus { get; set; }
        public string CNAME { get; set; }
        public string Phones { get; set; }
    }
    public class RenameOrg//замена на сокращения в названиях
    {
            string newName = "";
            string newFullName = "";
            Dictionary<string, string> ToChange = new Dictionary<string, string>
            {
            {"акционерный коммерческий банк","АКБ"},
            {"инвестиционный акционерный банк","ИАБ"},
            {"акционерный банк","АБ"},
            {"акционерный коммерческий ипотечный банк","АКИБ"},
            {"акционерный коммерческий","АК"},
            {"коммерческий банк","КБ"},
            {"платежная небанковская кредитная организация","ПНКО"},
            {"расчетная небанковская кредитная организация","РНКО"},
            {"небанковская кредитная организация","НКО"},
            {"общество с ограниченной ответственностью","ООО"},
            {"публичное акционерное общество","ПАО"},
            {"открытое акционерное общество","ОАО"},
            {"акционерное общество","АО"}
            };

            public string RenameThis(string name, string fullName)
        {
            newName = "";
            newFullName = fullName.ToLower();
            int start = 0;
            int finish = 0;
            while (newFullName.IndexOf("\"", finish+1)>=0)
            {
                start = finish;
                finish = newFullName.IndexOf('\"', finish+1);
            }
            if (start != finish)
            {
                newName = newFullName.Substring(start, ((finish - start) + 1));
                newFullName=newFullName.Substring(0, start) + newFullName.Substring(finish);
            }
            else newName = name; 

            foreach (string nameToChange in ToChange.Keys) //замена
            {
                int posFind = newFullName.IndexOf(nameToChange);
                if (posFind >= 0) 
                {
                    newName = ToChange[nameToChange]+ " " + newName;
                    newFullName = newFullName.Substring(0, posFind) + newFullName.Substring(posFind + nameToChange.Length);
                }
            }
            return (newName.ToUpper());
        }
    }
    public class RegionBase
    {
        Dictionary<decimal, string> regCodes = new Dictionary<decimal, string>();
        //грузим коды регионов
        ServiceReference1.CreditOrgInfoSoapClient sc { get; set; }
        public RegionBase(ServiceReference1.CreditOrgInfoSoapClient sc)
        {

        DataSet dsReg = sc.RegionsEnum();//получение сокращенного списка регионов
        var rowsReg = dsReg.Tables[0].Rows;//создаем объект из строк
        decimal keyOne;  
            Console.WriteLine("Дублирующиеся записи в перечне кодов регионов:");
            for (int i = 0; i<rowsReg.Count; i++)
            {
                keyOne = Convert.ToDecimal(rowsReg[i]["RegCode"]);
                if (regCodes.ContainsKey(keyOne))//если коды дублируются, то добавляем значение к старому, иначе записываем новое
                {
                    Console.WriteLine(Convert.ToString(rowsReg[i]["RegCode"])+" : "+Convert.ToString(rowsReg[i]["CNAME"]));//дополняем словарь
                    string newvalue = regCodes[keyOne];
                    regCodes.Remove(keyOne); //старую запись зачищаем
                    newvalue += (", " + Convert.ToString(rowsReg[i]["CNAME"]));
                    regCodes.Add(Convert.ToDecimal(rowsReg[i]["RegCode"]), newvalue);//добавляем новую из двух
                }
                else
                {
                    regCodes.Add(Convert.ToDecimal(rowsReg[i]["RegCode"]), Convert.ToString(rowsReg[i]["CNAME"]));//заполняем словарь
                }
            } 
        }
        public string FindRegion(decimal code)
        {
            decimal codeOne = code;
            string codeResult = codeOne.ToString();
            if (regCodes.ContainsKey(codeOne)) codeResult = regCodes[codeOne];
         return codeResult;
        }
    }
}
