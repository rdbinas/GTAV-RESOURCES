using System;
using RageCoop.Core.Scripting;

namespace RageCoop.Resources.Race
{
    public static class Events
    {
        public static int StartCheckpointSequence = CustomEvents.Hash("RageCoop.Resources.Race.StartCheckpointSequence");
        public static int CheckpointPassed = CustomEvents.Hash("RageCoop.Resources.Race.CheckpointPassed");
        public static int CountDown = CustomEvents.Hash("RageCoop.Resources.Race.ShowCountDown");
        public static int JoinRace = CustomEvents.Hash("RageCoop.Resources.Race.JoinRace");
        public static int LeaveRace = CustomEvents.Hash("RageCoop.Resources.Race.LeaveRace");
        public static int Cheating = CustomEvents.Hash("RageCoop.Resources.Race.Cheating");
    }
}
