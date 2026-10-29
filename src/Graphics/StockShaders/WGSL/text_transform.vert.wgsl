// sdlgpu samplers=0 storage_textures=0 storage_buffers=1 uniform_buffers=1
struct ChunkData {
    MatrixTransform: mat4x4<f32>,
    Color: vec4<f32>,
    DistanceRange: f32,
    FontIndex: u32,
    Padding: vec2<f32>,
}

struct type_u002e_StructuredBuffer_u002e_ChunkData {
    member: array<ChunkData>,
}

struct type_u002e_UniformBlock {
    ViewProjectionMatrix: mat4x4<f32>,
}

struct VertexOutput {
    @location(0) member: vec2<f32>,
    @location(1) member_1: vec4<f32>,
    @location(2) member_2: f32,
    @location(3) @interpolate(flat) member_3: u32,
    @builtin(position) member_4: vec4<f32>,
}

@group(0) @binding(0) 
var<storage> ChunkDataBuffer: type_u002e_StructuredBuffer_u002e_ChunkData;
@group(1) @binding(0) 
var<uniform> UniformBlock: type_u002e_UniformBlock;
var<private> in_u002e_var_u002e_TEXCOORD0_1: vec2<f32>;
var<private> in_u002e_var_u002e_TEXCOORD1_1: vec2<f32>;
var<private> in_u002e_var_u002e_TEXCOORD2_1: u32;
var<private> out_u002e_var_u002e_TEXCOORD0_: vec2<f32>;
var<private> out_u002e_var_u002e_TEXCOORD1_: vec4<f32>;
var<private> out_u002e_var_u002e_TEXCOORD2_: f32;
var<private> out_u002e_var_u002e_TEXCOORD3_: u32;
var<private> global: vec4<f32> = vec4<f32>(0f, 0f, 0f, 1f);

fn main_1() {
    let _e16 = in_u002e_var_u002e_TEXCOORD0_1;
    let _e17 = in_u002e_var_u002e_TEXCOORD1_1;
    let _e18 = in_u002e_var_u002e_TEXCOORD2_1;
    let _e20 = UniformBlock.ViewProjectionMatrix;
    let _e25 = ChunkDataBuffer.member[_e18].MatrixTransform;
    let _e35 = ChunkDataBuffer.member[_e18].DistanceRange;
    let _e39 = ChunkDataBuffer.member[_e18].Color;
    let _e43 = ChunkDataBuffer.member[_e18].FontIndex;
    out_u002e_var_u002e_TEXCOORD0_ = _e17;
    out_u002e_var_u002e_TEXCOORD1_ = _e39;
    out_u002e_var_u002e_TEXCOORD2_ = _e35;
    out_u002e_var_u002e_TEXCOORD3_ = _e43;
    global = (vec4<f32>(_e16.x, _e16.y, 0f, 1f) * (transpose(_e25) * transpose(_e20)));
    return;
}

@vertex 
fn main(@location(0) in_u002e_var_u002e_TEXCOORD0_: vec2<f32>, @location(1) in_u002e_var_u002e_TEXCOORD1_: vec2<f32>, @location(2) in_u002e_var_u002e_TEXCOORD2_: u32) -> VertexOutput {
    in_u002e_var_u002e_TEXCOORD0_1 = in_u002e_var_u002e_TEXCOORD0_;
    in_u002e_var_u002e_TEXCOORD1_1 = in_u002e_var_u002e_TEXCOORD1_;
    in_u002e_var_u002e_TEXCOORD2_1 = in_u002e_var_u002e_TEXCOORD2_;
    main_1();
    let _e11 = out_u002e_var_u002e_TEXCOORD0_;
    let _e12 = out_u002e_var_u002e_TEXCOORD1_;
    let _e13 = out_u002e_var_u002e_TEXCOORD2_;
    let _e14 = out_u002e_var_u002e_TEXCOORD3_;
    let _e15 = global;
    return VertexOutput(_e11, _e12, _e13, _e14, _e15);
}
