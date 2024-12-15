using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace BoidsSimulation.Source
{
    public struct Transform
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
    }
    
    public class Boid
    {
        public Transform Transform;
        
        public Boid(Vector2 position)
        {
            Transform = new Transform
            {
                Position = position,
                Velocity = new Vector2(
                    (float)(new Random().NextDouble() * 2 - 1),
                    (float)(new Random().NextDouble() * 2 - 1)
                ) * BoidForces.MaxSpeed
            };
            UpdateRotation();
        }

        public void Update(List<Boid> boids, Vector2 target, GameTime gameTime, Grid grid)
        {
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            var neighbors = grid.GetNearbyBoids(this, BoidForces.PerceptionRadius);
            
            var separation = CalculateSeparation(neighbors);
            var alignment = CalculateAlignment(neighbors);
            var cohesion = CalculateCohesion(neighbors);
            
            var targetForce = CalculateTargetForce(target);
            
            var acceleration = Vector2.Zero;
            acceleration += separation * BoidForces.SeparationWeight;
            acceleration += alignment * BoidForces.AlignmentWeight;
            acceleration += cohesion * BoidForces.CohesionWeight;
            acceleration += targetForce * BoidForces.TargetWeight;
            
            acceleration += new Vector2(
                (float)(new Random().NextDouble() * 2 - 1),
                (float)(new Random().NextDouble() * 2 - 1)
            ) * BoidForces.MaxForce * 0.1f;
            
            Transform.Velocity += acceleration * deltaTime;
            if (Transform.Velocity.Length() > BoidForces.MaxSpeed)
            {
                Transform.Velocity.Normalize();
                Transform.Velocity *= BoidForces.MaxSpeed;
            }
            Transform.Position += Transform.Velocity * deltaTime;
            UpdateRotation();
        }
        
        private void UpdateRotation() => Transform.Rotation =
            (float)Math.Atan2(Transform.Velocity.Y, Transform.Velocity.X);

        private Vector2 CalculateTargetForce(Vector2 target)
        {
            var desired = target - Transform.Position;
            var distance = desired.Length();
            
            var targetInfluence = 1.0f;
            if (distance < BoidForces.TargetInfluenceRadius)
            {
                targetInfluence = MathHelper.Lerp(BoidForces.MinTargetForce, 1.0f, 
                    distance / BoidForces.TargetInfluenceRadius);
            }

            if (!(distance > 0)) return Vector2.Zero;
            
            desired.Normalize();
            desired *= BoidForces.MaxSpeed * targetInfluence;
            var steer = desired - Transform.Velocity;
            
            return LimitForce(steer);
        }
        
        private Vector2 CalculateSeparation(List<Boid> neighbors)
        {
            var steer = Vector2.Zero;
            var count = 0;

            foreach (var other in neighbors)
            {
                var distance = Vector2.Distance(Transform.Position, other.Transform.Position);
                
                if (!(distance > 0) || !(distance < BoidForces.SeparationDistance)) continue;
                
                var diff = Transform.Position - other.Transform.Position;
                diff.Normalize();
                
                diff /= distance * distance;
                steer += diff;
                count++;
            }

            if (count <= 0) return steer;
            
            steer /= count;
            
            if (!(steer.Length() > 0)) return steer;
            
            steer.Normalize();
            steer *= BoidForces.MaxSpeed;
            steer -= Transform.Velocity;
            
            return LimitForce(steer);
        }

        private Vector2 CalculateAlignment(List<Boid> neighbors)
        {
            var averageVelocity = Vector2.Zero;
            var count = 0;

            foreach (var other in neighbors)
            {
                averageVelocity += other.Transform.Velocity;
                count++;
            }

            if (count <= 0) return averageVelocity;
            
            averageVelocity /= count;
            averageVelocity = LimitForce(averageVelocity);

            return averageVelocity;
        }

        private Vector2 CalculateCohesion(List<Boid> neighbors)
        {
            var centerOfMass = Vector2.Zero;
            var count = 0;

            foreach (var other in neighbors)
            {
                centerOfMass += other.Transform.Position;
                count++;
            }

            if (count <= 0) return Vector2.Zero;
            
            centerOfMass /= count;
            var desired = centerOfMass - Transform.Position;
            
            return LimitForce(desired);
        }

        private static Vector2 LimitForce(Vector2 force)
        {
            if (!(force.Length() > BoidForces.MaxForce)) return force;
            
            force.Normalize();
            force *= BoidForces.MaxForce;
            
            return force;
        }
    }
}
