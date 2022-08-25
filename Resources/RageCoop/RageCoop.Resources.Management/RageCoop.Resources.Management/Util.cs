using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using Newtonsoft.Json;
using RageCoop.Server;
using RageCoop.Core;

namespace RageCoop.Resources.Management
{
    internal static class Util
    {
        private static Logger _logger;
        public static void Init(Logger logger)
        {
            _logger = logger;
        }
        public static bool HasPermissionFlag(this PermissionFlags flagToCheck, PermissionFlags flag)
        {
            if(flag == PermissionFlags.All) { return flagToCheck == PermissionFlags.All; }
            return (flagToCheck & flag)!=0;
        }
        public static void Message(this Client c,string message)
        {
            if (c != null)
            {
                c.SendChatMessage(message);
            }
            else
            {
                _logger.Info(message);
            }
        }
    }
}
