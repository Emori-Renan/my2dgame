using MyGame.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class TilePrefabMapping
{
    public TileType type;
    public GameObject visualPrefab; // Retain if you use this for props or visuals
    public TileBase tileBase;
}