#pragma once

///////////////////////////////////////////////////////////////////////////////////////
// Phase functions for scattering 
///////////////////////////////////////////////////////////////////////////////////////

// 3 / (16 * pi)
#define PI316 0.0596831
// 1 / (4 * pi)
#define PI14 0.07957747

// Henyey-Greenstein phase function factor [-1, 1]
// represents the average cosine of the scattered directions
// 0 is isotropic scattering
// > 1 is forward scattering, < 1 is backwards
#define G 0.76

// Schlick phase function factor
#define K 1.55 * G - 0.55 * (G * G * G)

float GetRayleighPhase(float cosTheta)
{
    return PI316 * (1.0 + pow(cosTheta, 2.0));
}

float GetHenyeyGreensteinPhase(float cosTheta, float g)
{
    return PI14 * ((1.0 - g * g) / pow(1.0 + g * g - 2.0 * g * cosTheta, 1.5));
}

float GetSchlickPhase(float cosTheta, float k)
{
    return PI14 * ((1.0 - k * k) / pow(1.0 + k * cosTheta, 2.0));
}

///////////////////////////////////////////////////////////////////////////////////////
// in-scattering and extinction factor calculation
///////////////////////////////////////////////////////////////////////////////////////

float3 CalculateFinalExtinction(float3 viewDir, float kr, float3 br, float km, float3 bm)
{
    const float zenith = acos(saturate(dot(float3(0.0, 1.0, 0.0), viewDir)));

    // The optical length m = \frac{1}{\cos(\theta_s) + 0.15(93.885 - \theta_s)^{-1.253}}
    // Iqbal, M. An Introduction to Solar Radiation. Academic Press, 1983.
    const float s = 1 / (cos(zenith) + 0.15 * pow(abs(93.885 - ((zenith * 180.0) / PI)), -1.253));
    // The optical length of the atmosphere for air molecules: 8.4 km
    const float sr = kr * s;
    // The optical length of the atmosphere for haze particle: 1.25 km
    const float sm = km * s;

    // extinction coefficient for Air: Rayleigh scattering coefficient
    // extinction coefficient for Haze: Mie scattering + Absorption coefficient
    // calculate extinction factor using Beer's Law
    return exp(-(br * sr + bm * sm));
}