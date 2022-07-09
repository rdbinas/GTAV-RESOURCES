using System;
using RageCoop.Core.Scripting;

namespace RageCoop.Resources.Race
{
    public static class Events
    {
        public static int StartCheckpointSequence = CustomEvents.Hash("RageCoop.Resources.Race.StartCheckpointSequence");
        public static int CountDown = CustomEvents.Hash("RageCoop.Resources.Race.ShowCountDown");

    }
}
