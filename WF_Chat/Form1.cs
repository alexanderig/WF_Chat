using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WF_Chat
{
    public partial class Form1 : Form
    {

        public SynchronizationContext uiContext;
      
        List<string> clients;
        public Form1()
        {
            InitializeComponent();
            uiContext = SynchronizationContext.Current;
            clients = new List<string>();
            labelIP.Text = GetRightIp();
            WaitClientQuery();
            HelloFunc("777Hello");
        }

        private async void WaitClientQuery()
        {
            await Task.Run(() =>
            {
                try
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any,49152);

                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.Bind(ipEndPoint);
                    socket.EnableBroadcast = true;
                    while (true)
                    {
                        EndPoint remote = new IPEndPoint(0x7F000000, 100);
                        byte[] arr = new Byte[1024];
                        socket.ReceiveFrom(arr, ref remote);
                        string clientIP = ((IPEndPoint)remote).Address.ToString();

                        MemoryStream stream = new MemoryStream(arr);
                        BinaryFormatter formatter = new BinaryFormatter();
                        string[] m = (string[])formatter.Deserialize(stream);
                        
                        if(!clients.Contains(clientIP))
                        {
                            clients.Add(clientIP);
                            uiContext.Send(t => listBox1.Items.Add(m[1]), null);
                            HelloFunc("777Hello", clientIP);                            
                        }
                        else if (m[0] == "Close99")
                        {
                            uiContext.Send(t => {
                                for(int i = 0; i < clients.Count; i++)
                                {
                                    if (clients[i].Contains(clientIP))
                                    { clients.RemoveAt(i); break; }                                    
                                }
                                for (int i = 0; i < listBox1.Items.Count; i++)
                                {
                                    if (listBox1.Items[i].ToString().Contains(m[1]))
                                    { listBox1.Items.RemoveAt(i); break; }
                                }
                                listBox1.Refresh();
                                if (clientIP == textBox1.Text)
                                    textBox1.Text = "";
                            } , null);
                        }
                        else if (m[0] != "777Hello" && clientIP != GetRightIp())
                        {
                            uiContext.Send(d => {
                                listBox2.Items.Add("IP:" + clientIP + " Time: " + DateTime.Now);
                                listBox2.Items.Add("Host name: " + m[1]);
                                listBox2.Items.Add(m[0]);
                                listBox2.Items.Add("");
                                listBox2.TopIndex = listBox2.Items.Count - 1;                                
                            }, null);
                        }
                        stream.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Получатель: " + ex.Message);
                }
            });
        }

        // отправление сообщения
        private async void button1_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                try
                {
                    string ipaddress = string.Empty;
                    if (textBox1.Text.Length < 7 || textBox2.Text.Length < 1)
                        return;
                    if (checkBox1.Checked && textBox1.Text.Length > 7)
                    {
                        ipaddress = textBox1.Text.Substring(0, textBox1.Text.LastIndexOf('.') + 1);
                        ipaddress += "255";
                    }
                    else
                        ipaddress = textBox1.Text;
                    


                    IPEndPoint ipEndPoint = new IPEndPoint(
                        IPAddress.Parse(ipaddress), 49152);
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.EnableBroadcast = true;
                    MemoryStream stream = new MemoryStream();
                    BinaryFormatter formatter = new BinaryFormatter();
                    string[] m = new string[2];
                    m[0] = textBox2.Text;
                    m[1] = Environment.UserDomainName + @"\" + Environment.UserName;
                    formatter.Serialize(stream, m);
                    byte[] arr = stream.ToArray(); 
                    stream.Close();
                    socket.SendTo(arr, ipEndPoint);
                    socket.Shutdown(SocketShutdown.Send);
                    socket.Close();
                    uiContext.Send(t => { listBox2.Items.Add("Me at " + DateTime.Now); listBox2.Items.Add(textBox2.Text); textBox2.Text = ""; }, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Отправитель: " + ex.Message);
                }
            });
        }

        private async void HelloFunc(string mess, string senderip = "9999")
        {
            await Task.Run(() =>
            {
                try
                {
                    string ipaddress = GetRightIp();
                    ipaddress = ipaddress.Substring(0, ipaddress.LastIndexOf('.') + 1);
                    ipaddress += "255";
                    if (!senderip.Contains("9999"))
                        ipaddress = senderip;
                    IPEndPoint ipEndPoint = new IPEndPoint(
                        IPAddress.Parse(ipaddress), 49152);

                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.EnableBroadcast = true;
                    MemoryStream stream = new MemoryStream();
                    BinaryFormatter formatter = new BinaryFormatter();
                    string[] m = new string[2];
                    m[0] = mess;
                    m[1] = Environment.UserDomainName + @"\" + Environment.UserName;
                    formatter.Serialize(stream, m);
                    byte[] arr = stream.ToArray();
                    stream.Close();
                    socket.SendTo(arr, ipEndPoint);
                    socket.Shutdown(SocketShutdown.Send);
                    socket.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Отправитель: " + ex.Message);
                }
            });
        }



        private string GetRightIp()
        {
            try
            {
                using (var s = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    s.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0));
                    s.Connect("google.com", 0);
                    var ipaddr = s.LocalEndPoint as System.Net.IPEndPoint;
                    return ipaddr.Address.ToString();

                }
            }
            catch(Exception ex)
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                throw new Exception("No network adapters with an IPv4 address in the system!");
            }        
               
           
                
            


        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            HelloFunc("Close99");
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(listBox1.SelectedIndex > -1)
            if(listBox1.Items.Count > 0 && clients.Count > 0)
            {
                textBox1.Text = clients.ElementAt(listBox1.SelectedIndex);
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                button1_Click(this, EventArgs.Empty);
        }
    }
}
