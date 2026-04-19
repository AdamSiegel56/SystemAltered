#ifndef BREATHING_DISPLACEMENT_INCLUDED
#define BREATHING_DISPLACEMENT_INCLUDED

void BreathingDisplacement_float(
    float3 Position,
    float3 Normal,
    float BreathingIntensity,
    float BreathingScale,
    out float3 OutPosition
)
{
    float displacement = BreathingIntensity * BreathingScale;
    OutPosition = Position + Normal * displacement;
}

#endif