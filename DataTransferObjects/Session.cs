using SVN.FritzBoxApi.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;

namespace SVN.FritzBoxApi.DataTransferObjects
{
    internal class Session
    {
        public string SID { get; private set; }
        public string Challenge { get; private set; }
        public int BlockTime { get; private set; }
        public List<SessionRight> Rights { get; private set; }

        public bool IsConnected
        {
            get => this.SID != Constants.DefaultSID;
        }

        private Session()
        {
        }

        private static Session FromDocument(XDocument document)
        {
            var sid = document.GetValue<string>("SID") ?? Constants.DefaultSID;
            var challenge = document.GetValue<string>("Challenge") ?? string.Empty;
            var blockTime = document.GetValue<int>("BlockTime");
            var rights = new List<SessionRight>();

            var rightName = string.Empty;
            var rightAccess = default(SessionRightAccess);
            foreach (var element in document.GetChilds("Rights"))
            {
                var name = element.Name;

                if (name == "Name")
                {
                    rightName = element.GetValue<string>();
                }
                if (name == "Access")
                {
                    rightAccess = (SessionRightAccess)element.GetValue<int>();
                }
                if (rightName != string.Empty && rightAccess != default(SessionRightAccess))
                {
                    rights.Add(new SessionRight(rightName, rightAccess));
                    rightName = string.Empty;
                    rightAccess = default(SessionRightAccess);
                }
            }

            for (var i = default(int); i <= blockTime; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            return new Session
            {
                SID = sid,
                Challenge = challenge,
                BlockTime = blockTime,
                Rights = rights,
            };
        }

        public static Session FromSID(string url, string sid)
        {
            var document = XDocument.Load($"{url}/login_sid.lua?sid={sid}");
            return Session.FromDocument(document);
        }

        public static Session FromResponse(string url, string username, string response)
        {
            var document = XDocument.Load($"{url}/login_sid.lua?username={username}&response={response}");
            return Session.FromDocument(document);
        }

        public static Session Destroy(string url, string sid)
        {
            var document = XDocument.Load($"{url}/login_sid.lua?logout=1&sid={sid}");
            return Session.FromDocument(document);
        }

        public override string ToString()
        {
            return $"SID: {this.SID}, Challenge: {this.Challenge}, BlockTime: {this.BlockTime}, Rights: {this.Rights.Count}";
        }
    }
}