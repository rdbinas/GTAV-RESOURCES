using RageCoop.Server.Scripting;
using RageCoop.Core;
using System.IO;
using System.Collections.Generic;

namespace RageCoop.Resources.HandlingEnforcer
{
    public class Main : ServerScript
    {
        public override void OnStart()
        {
            API.RegisterSharedFile("handling.json", Path.Combine(CurrentResource.DataFolder, "handling.json"));
        }

        public override void OnStop()
        {
            
        }
    }
}