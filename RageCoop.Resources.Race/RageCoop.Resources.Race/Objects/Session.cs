using RageCoop.Server;

namespace RageCoop.Resources.Race.Objects
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
        public ServerVehicle Vehicle;
        public int CheckpointsPassed;
        public int CurrentCheckpoint;

        public Player(Client client)
        {
            Client = client;
            CheckpointsPassed = 0;
        }
    }
}
