using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.IO;

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

        private void SaveXml(XmlDocument document, string name)
        {
            using (StreamWriter sw = new StreamWriter(PATH + name, true))
            {
                sw.Write(document.OuterXml.ToString());
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
                    await HandleTcpClient(tcpClient, listener);
                }
            });
        }

        private async Task HandleTcpClient(TcpClient client, TcpListener listener)
        {
            WriteToLog("New Client");
            XmlDocument req1 = new XmlDocument();
            using (StreamReader sr = new StreamReader(client.GetStream()))
            {
                req1.LoadXml(sr.ReadToEnd()); 
            }
            SaveXml(req1, "Request-1.xml");
            XmlDocument resp1 = CreateResponceOne(req1);
            SaveXml(resp1, "Responce-1.xml");
            XmlDocument req2 = TcpConnection(resp1, listener);
            SaveXml(req2, "Request-2.xml");
        }

        private XmlDocument TcpConnection(XmlDocument doc, TcpListener listener)
        {
            var client = listener.AcceptTcpClient();
            XmlDocument resp = new XmlDocument();
            using (StreamWriter sw = new StreamWriter(client.GetStream()))
            {
                sw.Write(doc.OuterXml);
            }
            client = listener.AcceptTcpClient();
            using (StreamReader sr = new StreamReader(client.GetStream()))
            {
                resp.LoadXml(sr.ReadLine());
            }
            return resp;
        }

        private XmlDocument CreateResponceOne(XmlDocument document)
        {
            XmlElement digest = document.CreateElement("digest");
            document.AppendChild(digest);
            XmlText randomSequence = document.CreateTextNode(GenerateDigest());
            digest.AppendChild(randomSequence);
            return document;
        }

        private void ExctractLogin(XmlDocument doc)
        {

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
