using RageCoop.Core.Scripting;

namespace RageCoop.Resources.GangWars
{
    public static class Events
    {
        public static int Start = CustomEvents.Hash("RageCoop.Resources.GangWars.Start");
        public static int Stop = CustomEvents.Hash("RageCoop.Resources.GangWars.Stop");
        public static int MissionAccomplished = CustomEvents.Hash("RageCoop.Resources.GangWars.MissionAccomplished");
    }
}
