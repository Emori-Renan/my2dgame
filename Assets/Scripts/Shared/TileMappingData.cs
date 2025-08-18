using UnityEngine;
using MyGame.Core; // This line is required to find TileType

namespace MyGame.Shared
{
    [System.Serializable]
    public struct TileMappingData
    {
        public TileType tileType;
        public GameObject prefab;
    }
}