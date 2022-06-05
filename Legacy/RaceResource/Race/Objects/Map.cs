using CoopServer;
using System.Xml.Serialization;

namespace Race.Objects
{
    [XmlRoot(ElementName = "Race")]
    public class Map
    {
        [XmlArrayItem(ElementName = "Vector3")]
        public LVector3[] Checkpoints;
        public SpawnPoint[] SpawnPoints;
        public VehicleHash[] AvailableVehicles;
        public SavedProp[] DecorativeProps;

        public string Description;
        public string Name;

        public Map() { }
    }

    public class SpawnPoint
    {
        public LVector3 Position;
        public float Heading;
    }

    public class SavedProp
    {
        public LVector3 Position;
        public LVector3 Rotation;
        public int Hash;
        public bool Dynamic;
        public int Texture;
    }
}
