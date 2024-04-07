using Model;
using Model.Runtime.ReadOnly;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utilities;

public class UnitController : IDisposable
{
    private IReadOnlyRuntimeModel _runtimeModel;
    private TimeUtil _timeUtil;
    private static UnitController _controller;
    private bool _onPlayerHalf;
    private float _attackRange;
    private UnitSorter _unitSorter;
    public Vector2Int RecomendTarget {  get; private set; }
    public Vector2Int RecomendPoint {  get; private set; }

    private UnitController() 
    { 
        _runtimeModel = ServiceLocator.Get<IReadOnlyRuntimeModel>();
        _timeUtil = ServiceLocator.Get<TimeUtil>();
        _attackRange = _runtimeModel.RoPlayerUnits.First().Config.AttackRange;
        _unitSorter = new UnitSorter();

        _timeUtil.AddFixedUpdateAction(UpdateRecomendations);
    }

    public static UnitController GetInstance()
    {
        if(_controller == null)
            _controller = new UnitController();

        return _controller;
    }

    private void UpdateRecomendations(float deltaTime)
    {
        var botUnits = _runtimeModel.RoBotUnits.ToList();

        if (botUnits.Any())
        {
            CheckBorder();
            UpdateRecommendedTarget(botUnits);
            UpdateRecommendedPoint(botUnits);
            return;
        }

        var botBasePosition = _runtimeModel.RoMap.Bases[RuntimeModel.BotPlayerId];
        RecomendTarget = botBasePosition;
        RecomendPoint = botBasePosition;
    }

    private void UpdateRecommendedTarget(List<IReadOnlyUnit> botUnits)
    {
        if (_onPlayerHalf)
        {
            _unitSorter.SortByDistanceToBase(botUnits, EBaseType.PlayerBase);
        }
        else
        {
            _unitSorter.SortByHealth(botUnits);
        }

        RecomendTarget = botUnits.First().Pos;
    }


    public void UpdateRecommendedPoint(List<IReadOnlyUnit> botUnits)
    {
        if (_onPlayerHalf)
        {
            RecomendPoint = _runtimeModel.RoMap.Bases[RuntimeModel.PlayerId] + Vector2Int.up;
        }
        else
        {
            _unitSorter.SortByDistanceToBase(botUnits, EBaseType.PlayerBase);
            int x = botUnits.First().Pos.x;
            int y = botUnits.First().Pos.y - Mathf.FloorToInt(_attackRange);
            RecomendPoint = new Vector2Int(x, y);
        }
    }


    private void CheckBorder()
    {
        foreach (var unit in _runtimeModel.RoBotUnits)
        {
            if (BorderIsCrossed(unit.Pos))
            {
                _onPlayerHalf = true;
                return;
            }
        }

        _onPlayerHalf = false;
    }


    private bool BorderIsCrossed(Vector2Int botPos)
    {
        int playerBaseY = _runtimeModel.RoMap.Bases[RuntimeModel.PlayerId].y;
        int botBaseY = _runtimeModel.RoMap.Bases[RuntimeModel.BotPlayerId].y;
        int pointsToBorder = (botBaseY - playerBaseY) / 2;

        return botBaseY - botPos.y > pointsToBorder;
    }

    public void Dispose()
    {
        _timeUtil.RemoveFixedUpdateAction(UpdateRecomendations);
    }
}
