using System;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using System.IO;
using System.Security.Cryptography;

namespace TcpServer
{
    public partial class Service1 : ServiceBase
    {
        const string PATH = @"D:\Programming\SysProg\Labs\Lab4\Documents\";
        bool operating = true;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteToLog("TCP Service Started");
            Thread thread = new Thread(WorkingCycle);
            thread.Start();
        }

        protected override void OnStop()
        {
            WriteToLog("TCP Service Closed");
            operating = false;
        }

        private void SaveXml(XDocument document, string name)
        {
            using (StreamWriter sw = new StreamWriter(PATH + name, false))
            {
                sw.Write(document.ToString());
            }
        }

        private void WriteToLog(string message)
        {
            using (StreamWriter sw = new StreamWriter(PATH + "log.txt", true))
            {
                sw.WriteLine(DateTime.Now + " | " + message);
            }
        }

        private void WorkingCycle()
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("192.168.0.100"), 47000);
            listener.Start();

            _ = Task.Run(async () =>
            {
                while (operating)
                {
                    var tcpClient = await listener.AcceptTcpClientAsync();
                    HandleTcpClient(tcpClient, listener);
                }
            });
        }

        private void HandleTcpClient(TcpClient client, TcpListener listener)
        {
            try
            {
                WriteToLog("New Client");

                StreamReader sr = new StreamReader(client.GetStream());
                StreamWriter sw = new StreamWriter(client.GetStream());

                XDocument req1 = XDocument.Parse(sr.ReadLine());
                SaveXml(req1, "Request-1.xml");

                XDocument resp1 = CreateResponceOne(req1);
                SaveXml(resp1, "Responce-1.xml");
                sw.WriteLine(resp1.ToString().Replace(Environment.NewLine, ""));
                sw.Flush();

                XDocument req2 = XDocument.Parse(sr.ReadLine());
                SaveXml(req2, "Request-2.xml");

                XDocument resp2 = AuthenticateUser(req2, resp1);
                SaveXml(resp2, "Responce-2.xml");
                sw.WriteLine(resp2.ToString().Replace(Environment.NewLine, ""));
                sw.Flush();

                sw.Close();
                sr.Close();
                client.Close();
            }
            catch (Exception e)
            {
                WriteToLog(e.Message);
            }
        }

        private XDocument AuthenticateUser(XDocument req2, XDocument resp1)
        {
            string digest = resp1.Element("body").Element("digest").Value;
            string login = req2.Element("body").Element("login").Value;
            string hash = req2.Element("body").Element("hash").Value;
            Users list = new Users();
            User user = GetUser(login, list);
            if (user != null && CheckHash(user.pass, digest, hash))
            {
                return CorrectUser(login, user.group);
            }
            return LoginError();
        }

        private XDocument LoginError()
        {
            XDocument doc = new XDocument();
            XElement root = new XElement("body");
            XElement auth = new XElement("auth", "0");
            doc.Add(root);
            root.Add(auth);
            return doc;
        }

        private XDocument CorrectUser(string login, string group)
        {
            XDocument doc = new XDocument();
            XElement root = new XElement("body");
            XElement auth = new XElement("auth", "1");
            XElement groupNode = new XElement("group", group);
            doc.Add(root);
            root.Add(auth);
            root.Add(groupNode);
            return doc;
        }

        private bool CheckHash(string pass, string digest, string hash)
        {
            return hash == GenerateHash(pass, digest);
        }

        private string GenerateHash(string pass, string digest)
        {
            string concat = pass + digest;
            var data = Encoding.UTF8.GetBytes(concat);
            var hashData = new SHA1Managed().ComputeHash(data);
            var hash = "";
            foreach (var b in hashData)
            {
                hash += b.ToString("X2");
            }
            return hash;
        }

        private User GetUser(string login, Users list)
        {
            foreach (User user in list.users)
            {
                if (user.name == login) return user;
            }
            return null;
        }

        private XDocument CreateResponceOne(XDocument doc)
        {
            XElement digest = new XElement("digest", GenerateDigest());
            XElement root = doc.Element("body");
            root.Add(digest);
            return doc;
        }

        private string GenerateDigest()
        {
            int length = 40;
            StringBuilder str_build = new StringBuilder();
            Random random = new Random();
            char letter;
            for (int i = 0; i < length; i++)
            {
                double flt = random.NextDouble();
                int shift = Convert.ToInt32(Math.Floor(25 * flt));
                letter = Convert.ToChar(shift + 65);
                str_build.Append(letter);
            }
            return str_build.ToString();
        }
    }
}
