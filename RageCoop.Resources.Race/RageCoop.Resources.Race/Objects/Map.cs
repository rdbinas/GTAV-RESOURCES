using GTA;
using GTA.Math;
using System.Xml.Serialization;

namespace RageCoop.Resources.Race.Objects
{
    [XmlRoot(ElementName = "Race")]
    public class Map
    {
        [XmlArrayItem(ElementName = "Vector3")]
        public Vector3[] Checkpoints;
        public SpawnPoint[] SpawnPoints;
        public VehicleHash[] AvailableVehicles;
        public SavedProp[] DecorativeProps;

        public string Description;
        public string Name;

        public Map() { }
    }

    public class SpawnPoint
    {
        public Vector3 Position;
        public float Heading;
    }

    public class SavedProp
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public int Hash;
        public bool Dynamic;
        public int Texture;
    }
}
