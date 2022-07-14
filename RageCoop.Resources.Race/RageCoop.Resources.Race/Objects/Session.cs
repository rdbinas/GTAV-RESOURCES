using RageCoop.Server;
using RageCoop.Server.Scripting;
using GTA.Math;

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

        /// <summary>
        /// Rank all player's position
        /// </summary>
        public void Rank()
        {
            var checkPoints = Map.Checkpoints;
            var ordered=Players.OrderByDescending(x=>
            {
                double score = x.CheckpointsPassed;
                if (x.CheckpointsPassed<checkPoints.Length)
                {
                    score-=x.Vehicle.Position.DistanceTo(checkPoints[x.CheckpointsPassed])*0.000001;
                }
                return score;
            }).ToArray();
            for(int i = 0; i<ordered.Length; i++)
            {
                ordered[i].Ranking=(ushort)(i+1);
            }
        }
    }

    public class Player
    {
        public Client Client;
        public int VehicleHash;
        public ServerVehicle Vehicle;
        public int CheckpointsPassed;
        public ushort Ranking = 1;

        public Player(Client client)
        {
            Client = client;
            CheckpointsPassed = 0;
        }
    }
}
