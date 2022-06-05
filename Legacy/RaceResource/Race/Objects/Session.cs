using CoopServer;

namespace Race.Objects
{
    public struct Session
    {
        public State State;
        public Dictionary<Client, string> Votes;
        public DateTime NextEvent;

        public List<Player> Players;
        public Map Map;
        public long RaceStart;
    }

    public class Player
    {
        public Client Client;
        public int VehicleHash;
        public int Vehicle;
        public int CheckpointsPassed;
        public int RememberedCheckpoint;
        public List<int> RememberedBlips;
        public List<int> RememberedProps;

        public Player(Client client)
        {
            Client = client;
            CheckpointsPassed = 0;
            RememberedBlips = new List<int>();
            RememberedProps = new List<int>();
        }
    }
}
