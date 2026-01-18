using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [SerializeField] private int _modPoints = 24;
    [SerializeField] private List<CardSlot> _cardDeckPlayerModelFinal;

    public event Action<int> OnModPointsChanged;

    public int ModPoints => _modPoints;

    public bool SpendPoints(int value)
    {
        if (value > _modPoints)
            return false;

        _modPoints -= value;
        OnModPointsChanged?.Invoke(_modPoints);
        return true;
    }

    public void RefundPoints(int value)
    {
        _modPoints += value;
        OnModPointsChanged?.Invoke(_modPoints);
    }
}