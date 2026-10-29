// sdlgpu samplers=3 storage_textures=0 storage_buffers=0 uniform_buffers=0
// Hand port of Source/video_yuv2rgba.frag: its GLSL uses combined image samplers, which naga's SPIR-V frontend rejects.
@group(2) @binding(0) var YTexture: texture_2d<f32>;
@group(2) @binding(1) var YSampler: sampler;
@group(2) @binding(2) var UTexture: texture_2d<f32>;
@group(2) @binding(3) var USampler: sampler;
@group(2) @binding(4) var VTexture: texture_2d<f32>;
@group(2) @binding(5) var VSampler: sampler;

@fragment
fn main(@location(0) texCoord: vec2<f32>) -> @location(0) vec4<f32> {
    var yuv = vec3<f32>(
        textureSample(YTexture, YSampler, texCoord).r,
        textureSample(UTexture, USampler, texCoord).r,
        textureSample(VTexture, VSampler, texCoord).r,
    );
    yuv += vec3<f32>(-0.0625, -0.5, -0.5);
    return vec4<f32>(
        dot(yuv, vec3<f32>(1.164, 0.000, 1.793)),
        dot(yuv, vec3<f32>(1.164, -0.213, -0.533)),
        dot(yuv, vec3<f32>(1.164, 2.112, 0.000)),
        1.0,
    );
}
