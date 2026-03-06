#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8) in;

layout(set = 0, binding = 0, std430) buffer Heights {
    float heights[];
};

layout(set = 0, binding = 1, std430) buffer Normals {
    float normals[];
};

layout(push_constant) uniform Params {
    int width;        // 4 bytes
    int depth;        // 4 bytes
    float scale;      // 4 bytes
    float amplitude;  // 4 bytes
    float offsetX;    // 4 bytes
    float offsetZ;    // 4 bytes
    vec2 _padding;    // 8 bytes
} params;

// --------------------------------------------------
// Perlin noise helpers
// --------------------------------------------------

vec2 fade(vec2 t) {
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

vec2 gradient(vec2 cell) {
    float angle = hash(cell) * 6.28318530718;
    return vec2(cos(angle), sin(angle));
}

float perlin(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);

    vec2 u = fade(f);

    // Corners
    float n00 = dot(gradient(i + vec2(0,0)), f - vec2(0,0));
    float n10 = dot(gradient(i + vec2(1,0)), f - vec2(1,0));
    float n01 = dot(gradient(i + vec2(0,1)), f - vec2(0,1));
    float n11 = dot(gradient(i + vec2(1,1)), f - vec2(1,1));

    float nx0 = mix(n00, n10, u.x);
    float nx1 = mix(n01, n11, u.x);

    return mix(nx0, nx1, u.y);
}

vec3 normal(float fx, float fz) {
    float hL = perlin(vec2(fx - params.scale, fz)) * params.amplitude;
    float hR = perlin(vec2(fx + params.scale, fz)) * params.amplitude;
    float hD = perlin(vec2(fx, fz - params.scale)) * params.amplitude;
    float hU = perlin(vec2(fx, fz + params.scale)) * params.amplitude;

    return normalize(vec3(hL - hR, 2.0, hD - hU));
}

void main() {
    uint x = gl_GlobalInvocationID.x;
    uint z = gl_GlobalInvocationID.y;

    if (x >= uint(params.width + 1) || z >= uint(params.depth + 1))
        return;

    uint index = z * (params.width + 1) + x;

    vec2 pos = vec2(x, z) * params.scale;

    float fx = (float(x) + params.offsetX) * params.scale;
    float fz = (float(z) + params.offsetZ) * params.scale;

    float height = perlin(vec2(fx, fz)) * params.amplitude;
    heights[index] = height;

    vec3 normalVec = normal(fx, fz);
    normals[index * 3] = normalVec.x;
    normals[index * 3 + 1] = normalVec.y;
    normals[index * 3 + 2] = normalVec.z;
}