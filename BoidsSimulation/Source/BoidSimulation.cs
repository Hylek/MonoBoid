using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BoidsSimulation.Source;

public class BoidSimulation : Game
{
    private const int ScreenWidth = 800;
    private const int ScreenHeight = 600;
    
    private const float CellSize = 40f;
    private BasicEffect _basicEffect;
    
    private List<Boid> _boids;
    private int _currentGridLineCount;
    private readonly GraphicsDeviceManager _graphics;
    private Grid _grid;
    
    private VertexPositionColor[] _gridLines;
    private Texture2D _pixel;
    private KeyboardState _prevKeyState;
    private bool _showDebug = true;
    private SpriteBatch _spriteBatch;
    private Vector2 _target;

    public BoidSimulation()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";

        _graphics.PreferredBackBufferWidth = ScreenWidth;
        _graphics.PreferredBackBufferHeight = ScreenHeight;

        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _basicEffect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            Projection = Matrix.CreateOrthographicOffCenter(
                0, ScreenWidth, ScreenHeight, 0, 0, 1),
            View = Matrix.Identity,
            World = Matrix.Identity
        };
        
        _grid = new Grid(CellSize, ScreenWidth, ScreenHeight);
        _boids = [];
        
        var rand = new Random();
        for (var i = 0; i < 200; i++)
        {
            _boids.Add(new Boid(new Vector2(rand.Next(ScreenWidth), rand.Next(ScreenHeight))));
        }
        
        _target = new Vector2(ScreenWidth / 2, ScreenHeight / 2);
        _gridLines = new VertexPositionColor[1000];

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    protected override void Update(GameTime gameTime)
    {
        var mouseState = Mouse.GetState();
        _target = new Vector2(mouseState.X, mouseState.Y);
        
        var keyState = Keyboard.GetState();
        if (keyState.IsKeyDown(Keys.Space) && !_prevKeyState.IsKeyDown(Keys.Space))
            _showDebug = !_showDebug;
        _prevKeyState = keyState;
        
        _grid.UpdateGrid(_boids);
        
        foreach (var boid in _boids)
        {
            boid.Update(_boids, _target, gameTime, _grid);
            WrapPosition(boid);
        }
        base.Update(gameTime);
    }

    private static void WrapPosition(Boid boid)
    {
        if (boid.Position.X < 0) boid.Position.X = ScreenWidth;
        if (boid.Position.X > ScreenWidth) boid.Position.X = 0;
        if (boid.Position.Y < 0) boid.Position.Y = ScreenHeight;
        if (boid.Position.Y > ScreenHeight) boid.Position.Y = 0;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        
        if (_showDebug)
        {
            DrawDebugGrid();
            DebugDrawBoidNeighborhoods();
        }
        
        _spriteBatch.Begin();
        
        DrawBoidTargetSquare();
        DrawBoids();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawBoids()
    {
        foreach (var boid in _boids) DrawBoid(boid);
    }

    private void DrawBoidTargetSquare()
    {
        _spriteBatch.Draw(_pixel, new Rectangle((int)_target.X - 5, (int)_target.Y - 5, 10, 10),
            Color.Yellow);
    }

    private void DrawBoid(Boid boid)
    {
        const float size = 10f;
        var triangle = new[]
        {
            new Vector2(-size, -size / 2),
            new Vector2(-size, size / 2),
            new Vector2(size, 0)
        };
        
        for (var i = 0; i < triangle.Length; i++)
        {
            var cos = (float)Math.Cos(boid.Rotation);
            var sin = (float)Math.Sin(boid.Rotation);
            var rotated = new Vector2(
                triangle[i].X * cos - triangle[i].Y * sin,
                triangle[i].X * sin + triangle[i].Y * cos
            );
            triangle[i] = rotated + boid.Position;
        }
        
        DrawTriangle(triangle[0], triangle[1], triangle[2], Color.LightBlue);
    }

    private void DrawDebugGrid()
    {
        _currentGridLineCount = 0;
        for (float x = 0; x <= ScreenWidth; x += CellSize)
            DebugAddGridLine(
                new Vector2(x, 0),
                new Vector2(x, ScreenHeight),
                Color.DarkGray * 0.5f
            );
        
        for (float y = 0; y <= ScreenHeight; y += CellSize)
            DebugAddGridLine(
                new Vector2(0, y),
                new Vector2(ScreenWidth, y),
                Color.DarkGray * 0.5f
            );
        
        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserPrimitives(
                PrimitiveType.LineList,
                _gridLines,
                0,
                _currentGridLineCount / 2
            );
        }
    }

    private void DebugDrawBoidNeighborhoods()
    {
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
        foreach (var boid in _boids) DrawCircle(boid.Position, BoidForces.PerceptionRadius, Color.Green * 1f);
        _spriteBatch.End();
    }

    private void DebugAddGridLine(Vector2 start, Vector2 end, Color color)
    {
        if (_currentGridLineCount + 2 > _gridLines.Length) return;
        
        _gridLines[_currentGridLineCount++] = new VertexPositionColor(
            new Vector3(start, 0), color);
        _gridLines[_currentGridLineCount++] = new VertexPositionColor(
            new Vector3(end, 0), color);
    }
    
    private void DrawCircle(Vector2 center, float radius, Color color)
    {
        const int resolution = 32;
        var points = new Vector2[resolution + 1];

        for (var i = 0; i <= resolution; i++)
        {
            var angle = i * MathHelper.TwoPi / resolution;
            points[i] = new Vector2(
                center.X + radius * (float)Math.Cos(angle),
                center.Y + radius * (float)Math.Sin(angle)
            );
        }

        for (var i = 0; i < resolution; i++) DrawLine(points[i], points[i + 1], color);
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color)
    {
        var edge = end - start;
        var angle = (float)Math.Atan2(edge.Y, edge.X);

        _spriteBatch.Draw(
            _pixel,
            start,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(edge.Length(), 1),
            SpriteEffects.None,
            0
        );
    }

    private void DrawTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Color color)
    {
        DrawLine(p1, p2, color);
        DrawLine(p2, p3, color);
        DrawLine(p3, p1, color);
    }
}
