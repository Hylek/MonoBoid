using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BoidsSimulation.Source;

public class Grid
{
    private readonly Dictionary<int, List<Boid>> _cells;
    private readonly float _cellSize;
    private readonly int _gridHeight;
    private readonly int _gridWidth;
    private readonly ObjectPool<List<Boid>> _listPool;

    public Grid(float cellSize, int width, int height)
    {
        _cellSize = cellSize;
        _gridWidth = (int)(width / cellSize) + 1;
        _gridHeight = (int)(height / cellSize) + 1;
        _cells = new Dictionary<int, List<Boid>>(_gridWidth * _gridHeight);
        
        _listPool = new ObjectPool<List<Boid>>(
            () => new List<Boid>(32),
            list => list.Clear(),
            list => list.Clear(),
            _gridWidth * _gridHeight / 4);
    }

    private int GetCellIndex(Vector2 position)
    {
        var x = (int)(position.X / _cellSize);
        var y = (int)(position.Y / _cellSize);
        return x + y * _gridWidth;
    }

    public void UpdateGrid(List<Boid> boids)
    {
        foreach (var list in _cells.Values) _listPool.Return(list);
        _cells.Clear();
        
        var cellAssignments = new List<(int cellIndex, Boid boid)>(boids.Count);
        for (var i = 0; i < boids.Count; i++)
        {
            var boid = boids[i];
            var cellIndex = GetCellIndex(boid.Transform.Position);
            cellAssignments.Add((cellIndex, boid));
        }

        cellAssignments.Sort((a, b) => a.cellIndex.CompareTo(b.cellIndex));
        
        foreach (var (cellIndex, boid) in cellAssignments)
        {
            if (!_cells.TryGetValue(cellIndex, out var list))
            {
                list = _listPool.Get();
                _cells[cellIndex] = list;
            }

            list.Add(boid);
        }
    }

    public List<Boid> GetNearbyBoids(Boid boid, float radius)
    {
        var nearby = new List<Boid>();
        var centerCell = GetCellIndex(boid.Transform.Position);
        var cellRadius = (int)(radius / _cellSize) + 1;
        var radiusSquared = radius * radius;

        for (var y = -cellRadius; y <= cellRadius; y++)
        {
            var cellY = centerCell / _gridWidth + y;
            if (cellY < 0 || cellY >= _gridHeight) continue;

            for (var x = -cellRadius; x <= cellRadius; x++)
            {
                var cellX = centerCell % _gridWidth + x;
                if (cellX < 0 || cellX >= _gridWidth) continue;

                var neighborCellIndex = cellX + cellY * _gridWidth;
                if (!_cells.TryGetValue(neighborCellIndex, out var cell)) continue;

                foreach (var other in cell)
                {
                    if (other == boid) continue;
                    var dx = other.Transform.Position.X - boid.Transform.Position.X;
                    var dy = other.Transform.Position.Y - boid.Transform.Position.Y;
                    if (dx * dx + dy * dy <= radiusSquared) nearby.Add(other);
                }
            }
        }
        return nearby;
    }
}