using SVN.FritzBoxApi.Enums;

namespace SVN.FritzBoxApi.DataTransferObjects
{
    internal class SessionRight
    {
        public string Name { get; }
        public SessionRightAccess Access { get; }

        public SessionRight(string name, SessionRightAccess access)
        {
            this.Name = name;
            this.Access = access;
        }
    }
}