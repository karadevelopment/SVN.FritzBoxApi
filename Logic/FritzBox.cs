using SVN.FritzBoxApi.DataTransferObjects;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

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
            get => this.Session?.SID ?? Constants.DefaultSID;
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

            if (this.Session.SID == Constants.DefaultSID)
            {
                var response = this.BuildResponse(this.Session.Challenge, password);
                this.Session = Session.FromResponse(this.URL, username, response);
            }
        }

        public void Logout()
        {
            this.Session = Session.Destroy(this.URL, this.SID);
        }

        private string ReadResource(string name)
        {
            // TODO
            return string.Empty;
        }

        private string ExecuteCommand(string cmd)
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

            var url = "http://fritz.box:49000/igdupnp/control/WANIPConn1";
            var xml = this.ReadResource($"{cmd}.xml");

            var request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";
            request.Headers.Add("SOAPACTION", $"urn:schemas-upnp-org:service:WANIPConnection:1#{cmd}");
            request.ProtocolVersion = HttpVersion.Version11;
            request.Credentials = CredentialCache.DefaultCredentials;
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
            return this.ExecuteCommand("GetExternalIPAddress");
        }

        public void Reconnect()
        {
            this.ExecuteCommand("ForceTermination");
        }

        public override string ToString()
        {
            return this.Session?.ToString() ?? $"SID: {Constants.DefaultSID}";
        }
    }
}