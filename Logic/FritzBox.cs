using SVN.FritzBoxApi.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SVN.FritzBoxApi.Logic
{
    public class FritzBox : IDisposable
    {
        private string URL { get; }
        private Session Session { get; set; }

        private string MacAddress
        {
            get => Utils.GetMacAddress();
        }

        private string SID
        {
            get => this.Session?.SID ?? Constants.SID_DEFAULT;
        }

        public FritzBox(string url = "http://fritz.box")
        {
            this.URL = url;
        }

        public void Dispose()
        {
            this.Logout();
        }

        private string BuildResponse(string challenge, string password)
        {
            var md5 = MD5.Create();
            var bytes = Encoding.Unicode.GetBytes($"{challenge}-{password}");
            var bytesMD5 = md5.ComputeHash(bytes);
            var builder = new StringBuilder();

            foreach (var bits in bytesMD5)
            {
                builder.Append($"{bits:x2}");
            }

            return $"{challenge}-{builder}";
        }

        public void Authenticate(string username, string password)
        {
            this.Session = Session.FromResponse(this.URL, username, password);

            if (this.Session.SID == Constants.SID_DEFAULT)
            {
                var response = this.BuildResponse(this.Session.Challenge, password);
                this.Session = Session.FromResponse(this.URL, username, response);
            }
        }

        public void Logout()
        {
            Session.Destroy(this.URL, this.SID);
            this.Session = null;
        }

        private IEnumerable<Stream> GetResources(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();

            foreach (var resource in resources.Where(x => x.EndsWith(name)))
            {
                var filename = resource;

                while (2 <= filename.Count(x => x == '.'))
                {
                    filename = filename.Substring(filename.IndexOf('.') + 1);
                }

                yield return assembly.GetManifestResourceStream(resource);
            }
        }


        private string ReadResource(string name)
        {
            foreach (var stream in this.GetResources(name))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }

            return string.Empty;
        }

        private string ExecuteCommand(string location, string action, string urn, string username, string password)
        {
            //var url = $"{this.URL}/webservices/homeautoswitch.lua?ain={this.MacAddress}&switchcmd={cmd}&sid={this.SID}";

            //try
            //{
            //    var request = WebRequest.Create(url) as HttpWebRequest;

            //    using (var response = request.GetResponse() as HttpWebResponse)
            //    using (var stream = response.GetResponseStream())
            //    using (var reader = new StreamReader(stream))
            //    {
            //        return reader.ReadToEnd();
            //    }
            //    return XDocument.Load(url).ToString();
            //}
            //catch (WebException e)
            //{
            //    switch (e.Status)
            //    {
            //        case WebExceptionStatus.ProtocolError:
            //            var eResponse = e.Response as HttpWebResponse;
            //            return $"{e.Status} - {eResponse.StatusCode}";
            //        default:
            //            return $"{e.Status}";
            //    }
            //}

            location = $"{this.URL}:49000/{location}";
            var soapaction = $"{urn}#{action}";
            var xml = this.ReadResource("Template.xml").Replace("{action}", action).Replace("{urn}", urn);

            var request = WebRequest.Create(location) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";
            request.Headers.Add("location", location);
            request.Headers.Add("uri", urn);
            request.Headers.Add("soapaction", soapaction);
            //request.Headers.Add("login", username);
            //request.Headers.Add("password", password);
            request.Headers.Add("noroot", "True");
            request.ProtocolVersion = HttpVersion.Version11;
            request.Credentials = new NetworkCredential(username, password);
            request.ContentLength = xml.Length;

            using (var stream = request.GetRequestStream())
            using (var writer = new StreamWriter(stream, Encoding.ASCII))
            {
                writer.Write(xml);
            }
            using (var response = request.GetResponse() as HttpWebResponse)
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public string GetExternalIPAddress()
        {
            var xml = this.ExecuteCommand("igdupnp/control/WANIPConn1", "GetExternalIPAddress", "urn:schemas-upnp-org:service:WANIPConnection:1", null, null);
            var value = xml.GetXmlValue<string>("NewExternalIPAddress");
            return value;
        }

        public void Reconnect()
        {
            this.ExecuteCommand("igdupnp/control/WANIPConn1", "ForceTermination", "urn:schemas-upnp-org:service:WANIPConnection:1", null, null);
        }

        public void Reboot(string username, string password)
        {
            this.ExecuteCommand("upnp/control/deviceconfig", "Reboot", "urn:dslforum-org:service:DeviceConfig:1", username, password);

            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));

                try
                {
                    var ip = this.GetExternalIPAddress();
                    
                    if (!string.IsNullOrWhiteSpace(ip))
                    {
                        return;
                    }
                }
                catch (WebException)
                {
                }
            }
        }

        public override string ToString()
        {
            return this.Session?.ToString() ?? $"SID: {Constants.SID_DEFAULT}";
        }
    }
}