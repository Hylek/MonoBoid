using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace BoidsSimulation.Source;

public class Grid(float cellSize, int width, int height)
{
    private readonly Dictionary<(int, int), List<Boid>> _cells = new();
    private int _gridWidth = (int)(width / cellSize) + 1;
    private int _gridHeight = (int)(height / cellSize) + 1;

    public void UpdateGrid(List<Boid> boids)
    {
        _cells.Clear();
        foreach (var boid in boids)
        {
            var cell = GetCell(boid.Position);
            if (!_cells.TryGetValue(cell, out var value))
            {
                value = ([]);
                _cells[cell] = value;
            }
            value.Add(boid);
        }
    }

    public List<Boid> GetNearbyBoids(Boid boid, float radius)
    {
        var nearby = new List<Boid>();
        var cell = GetCell(boid.Position);
        var cellRadius = (int)(radius / cellSize) + 1;

        for (var x = -cellRadius; x <= cellRadius; x++)
        {
            for (var y = -cellRadius; y <= cellRadius; y++)
            {
                var neighborCell = (cell.Item1 + x, cell.Item2 + y);
                if (!_cells.TryGetValue(neighborCell, out var cell1)) continue;
                    
                nearby.AddRange(cell1.Where(other => other != boid &&
                                                     Vector2.Distance(boid.Position, other.Position) <= radius));
            }
        }

        return nearby;
    }

    private (int, int) GetCell(Vector2 position) => ((int)(position.X / cellSize), (int)(position.Y / cellSize));
}