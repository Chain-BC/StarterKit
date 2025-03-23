using System.Collections.Generic;

namespace StarterKit
{
    public class Config
    {
        public bool requiresPrivilege = true;
        public string privilege = "starterkit";
        public bool removePrivilegeOnKitUse = true;
        public List<string[]> kitItems = new List<string[]>();
    }
}
