using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace TcpServer
{
    class Users
    {
        public List<User> users = new List<User>();
        public Users()
        {
            var data = GetRegistrySubKeys();
            foreach (var value in data)
            {
                users.Add(new User(value.Key, value.Value.Split(' ')[0], value.Value.Split(' ')[1]));
            }
        }

        private Dictionary<string, string> GetRegistrySubKeys()
        {
            var valuesBynames = new Dictionary<string, string>();
            const string REGISTRY_ROOT = @"SOFTWARE\CHAP-Server";
            //Here I'm looking under LocalMachine. You can replace it with Registry.CurrentUser for current user...
            using (RegistryKey rootKey = Registry.LocalMachine.OpenSubKey(REGISTRY_ROOT))
            {
                if (rootKey != null)
                {
                    string[] valueNames = rootKey.GetValueNames();
                    foreach (string currSubKey in valueNames)
                    {
                        object value = rootKey.GetValue(currSubKey);
                        valuesBynames.Add(currSubKey, (string)value);
                    }
                    rootKey.Close();
                }
            }
            return valuesBynames;
        }
    }

    class User
    {
        public string name;
        public string pass;
        public string group;

        public User(string name, string pass, string group)
        {
            this.name = name;
            this.pass = pass;
            this.group = group;
        }
    }
}
