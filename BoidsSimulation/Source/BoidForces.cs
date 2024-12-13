namespace BoidsSimulation.Source;

public static class BoidForces
{
    public const float MaxSpeed = 200f;
    public const float MaxForce = 70f;
    public const float PerceptionRadius = 25f;
    public const float SeparationDistance = 30f;
        
    public const float TargetInfluenceRadius = 200f;
    public const float MinTargetForce = 0.2f;
        
    public const float SeparationWeight = 2.5f;
    public const float AlignmentWeight = 1.0f;
    public const float CohesionWeight = 1.0f;
    public const float TargetWeight = 0.5f;
}