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

// 2D Random function
float rand(vec2 c){
    return fract(sin(dot(c.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

// 2D Smooth Noise
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    
    // Four corners of a tile
    float a = rand(i);
    float b = rand(i + vec2(1.0, 0.0));
    float c = rand(i + vec2(0.0, 1.0));
    float d = rand(i + vec2(1.0, 1.0));

    // Smooth interpolation (cubic curve)
    vec2 u = smoothstep(0.0, 1.0, f);

    // Mix the four corners
    return mix(a, b, u.x) +
            (c - a) * u.y * (1.0 - u.x) +
            (d - b) * u.x * u.y;
}

vec3 normal(float fx, float fz) {
    float hL = noise(vec2(fx - params.scale, fz)) * params.amplitude;
    float hR = noise(vec2(fx + params.scale, fz)) * params.amplitude;
    float hD = noise(vec2(fx, fz - params.scale)) * params.amplitude;
    float hU = noise(vec2(fx, fz + params.scale)) * params.amplitude;

    return normalize(vec3(hL - hR, 2.0, hD - hU));
}

// void main() {
//     uint x = gl_GlobalInvocationID.x;
//     uint z = gl_GlobalInvocationID.y;

//     if (x >= uint(params.width + 1) || z >= uint(params.depth + 1))
//         return;

//     uint index = z * (params.width + 1) + x;

//     vec2 pos = vec2(x, z) * params.scale;

//     float fx = (float(x) + params.offsetX) * params.scale;
//     float fz = (float(z) + params.offsetZ) * params.scale;

//     float height = noise(vec2(fx, fz)) * params.amplitude;
//     heights[index] = height;

//     // vec3 normalVec = normal(fx, fz);
//     normals[index * 3] = 1;
//     normals[index * 3 + 1] = 1;
//     normals[index * 3 + 2] = 1;
// }

void main() {
    uint x = gl_GlobalInvocationID.x;
    uint z = gl_GlobalInvocationID.y;

    float fx = (float(x) + params.offsetX) * params.scale;
    float fz = (float(z) + params.offsetZ) * params.scale;

    uint index = z * (params.width + 1) + x;

    float height = noise(vec2(fx, fz));
    heights[index] = height;

    vec3 normalVec = normal(fx, fz);
    normals[index * 3] = normalVec.x;
    normals[index * 3 + 1] = normalVec.y;
    normals[index * 3 + 2] = normalVec.z;
}