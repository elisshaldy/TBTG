using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MovementPattern
{
    public List<Vector2Int> RelativeDestinations;
    public bool CanRotate = true;
    public bool CanMirror = true;
}