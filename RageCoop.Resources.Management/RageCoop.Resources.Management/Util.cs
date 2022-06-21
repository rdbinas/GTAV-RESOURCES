using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using Newtonsoft.Json;

namespace RageCoop.Resources.Management
{
    internal static class Util
    {
        public static bool HasPermissionFlag(this PermissionFlags flagToCheck, PermissionFlags flag)
        {
            return (flagToCheck & flag)!=0;
        }
    }
}
