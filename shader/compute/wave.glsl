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

float height(vec2 pos) {
    return  sin(pos.x) * sin(pos.y);
}

void main() {
    uint x = gl_GlobalInvocationID.x;
    uint z = gl_GlobalInvocationID.y;

    if (x > params.width || z > params.depth) {
        return;
    }

    float fx = (float(x) + params.offsetX) / params.scale;
    float fz = (float(z) + params.offsetZ) / params.scale;

    uint index = z * (params.width + 1) + x;

    float height = height(vec2(fx, fz));
    heights[index] = height;

    normals[index * 3] = 1;
    normals[index * 3 + 1] = 1;
    normals[index * 3 + 2] = 1;
}