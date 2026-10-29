// sdlgpu samplers=0 storage_textures=0 storage_buffers=0 uniform_buffers=0
diagnostic(off, derivative_uniformity);
struct gl_PerVertex {
    @builtin(position) gl_Position: vec4<f32>,
    gl_PointSize: f32,
    gl_ClipDistance: array<f32, 1>,
    gl_CullDistance: array<f32, 1>,
}

struct VertexOutput {
    @location(0) member: vec2<f32>,
    @builtin(position) gl_Position: vec4<f32>,
}

var<private> outTexCoord: vec2<f32>;
var<private> gl_VertexIndex_1: i32;
var<private> unnamed: gl_PerVertex = gl_PerVertex(vec4<f32>(0f, 0f, 0f, 1f), 1f, array<f32, 1>(), array<f32, 1>());

fn main_1() {
    let _e14 = gl_VertexIndex_1;
    let _e19 = gl_VertexIndex_1;
    outTexCoord = vec2<f32>(f32(((_e14 << bitcast<u32>(1i)) & 2i)), f32((_e19 & 2i)));
    let _e23 = outTexCoord;
    let _e25 = ((_e23 * vec2<f32>(2f, -2f)) + vec2<f32>(-1f, 1f));
    unnamed.gl_Position = vec4<f32>(_e25.x, _e25.y, 0f, 1f);
    return;
}

@vertex 
fn main(@builtin(vertex_index) gl_VertexIndex: u32) -> VertexOutput {
    gl_VertexIndex_1 = i32(gl_VertexIndex);
    main_1();
    let _e6 = outTexCoord;
    let _e7 = unnamed.gl_Position;
    return VertexOutput(_e6, _e7);
}
