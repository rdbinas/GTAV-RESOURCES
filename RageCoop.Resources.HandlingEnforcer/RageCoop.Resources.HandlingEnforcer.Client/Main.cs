using System;
using System.Collections.Generic;
using RageCoop.Client.Scripting;
using System.Xml;
using Newtonsoft.Json;

namespace RageCoop.Resources.HandlingEnforcer.Client
{
    public class Main : ClientScript
    {
        XmlDocument Document;
        Dictionary<string, HandlingData> HandlingDatamn = new Dictionary<string, HandlingData>();
        Dictionary<GTA.HandlingData, HandlingData> ModifiedHandlings = new Dictionary<GTA.HandlingData, HandlingData>();
        public override void OnStart()
        {
            API.RequestSharedFile("RageCoop.Resources.HandlingEnforcer.Meta", Load);
            
        }
        void Load(string s)
        {
            if (s == null)
            {

                Logger.Info("null!");
            }
            Logger.Info("Reading handling data from "+s);
            Document = new XmlDocument();
            Document.Load(s);
            foreach (XmlNode node in Document.DocumentElement.ChildNodes[0].ChildNodes)
            {
                var data = new HandlingData(node);
                HandlingDatamn.Add(data.Name.ToUpper(), data);
            }
            API.Events.OnVehicleSpawned+=ApplyHandling;
        }
        private void ApplyHandling(object sender, RageCoop.Client.SyncedVehicle e)
        {
            if(e == null||e.MainVehicle==null) { return; }
            // Logger.Debug("Vehicle spawnd: "+e.MainVehicle.DisplayName.ToUpper());
            
            if (HandlingDatamn.TryGetValue(e.MainVehicle.DisplayName.ToUpper(),out var data))
            {
                var h = e.MainVehicle.HandlingData;
                if (!ModifiedHandlings.ContainsKey(h))
                {
                    Logger.Debug("Applying handling data to: "+e.MainVehicle.DisplayName);
                    
                    // Copy and store unmodified handling data
                    ModifiedHandlings.Add(h,new HandlingData(h));

                    data.ApplyTo(h);
                }
                // Logger.Trace(JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
                // Logger.Trace(JsonConvert.SerializeObject(new HandlingData(e.MainVehicle), Newtonsoft.Json.Formatting.Indented));

            }
        }

        public override void OnStop()
        {
            // Restore modified handling data
            foreach(var p in ModifiedHandlings)
            {
                p.Value.ApplyTo(p.Key);
            }
        }
    }
}