// sdlgpu samplers=4 storage_textures=0 storage_buffers=0 uniform_buffers=0
diagnostic(off, derivative_uniformity);
var<private> in_u002e_var_u002e_TEXCOORD0_1: vec2<f32>;
var<private> in_u002e_var_u002e_TEXCOORD1_1: vec4<f32>;
var<private> in_u002e_var_u002e_TEXCOORD2_1: f32;
var<private> in_u002e_var_u002e_TEXCOORD3_1: u32;
var<private> out_u002e_var_u002e_SV_Target0_: vec4<f32>;
@group(2) @binding(0) 
var msdf_u005b_0_u005d: texture_2d<f32>;
@group(2) @binding(2) 
var msdf_u005b_1_u005d: texture_2d<f32>;
@group(2) @binding(4) 
var msdf_u005b_2_u005d: texture_2d<f32>;
@group(2) @binding(6) 
var msdf_u005b_3_u005d: texture_2d<f32>;
@group(2) @binding(1) 
var msdfSampler_u005b_0_u005d: sampler;
@group(2) @binding(3) 
var msdfSampler_u005b_1_u005d: sampler;
@group(2) @binding(5) 
var msdfSampler_u005b_2_u005d: sampler;
@group(2) @binding(7) 
var msdfSampler_u005b_3_u005d: sampler;

fn main_1() {
    var phi_76_: vec4<f32>;
    var phi_99_: vec4<f32>;
    var phi_122_: vec4<f32>;
    var phi_145_: vec4<f32>;
    var phi_146_: vec4<f32>;
    var phi_168_: vec2<u32>;

    let _e22 = in_u002e_var_u002e_TEXCOORD0_1;
    let _e23 = in_u002e_var_u002e_TEXCOORD1_1;
    let _e24 = in_u002e_var_u002e_TEXCOORD2_1;
    let _e25 = in_u002e_var_u002e_TEXCOORD3_1;
    switch bitcast<i32>(_e25) {
        case 0: {
            switch bitcast<i32>(_e25) {
                case 0: {
                    let _e28 = textureSample(msdf_u005b_0_u005d, msdfSampler_u005b_0_u005d, _e22);
                    phi_76_ = _e28;
                    break;
                }
                case 1: {
                    let _e29 = textureSample(msdf_u005b_0_u005d, msdfSampler_u005b_1_u005d, _e22);
                    phi_76_ = _e29;
                    break;
                }
                case 2: {
                    let _e30 = textureSample(msdf_u005b_0_u005d, msdfSampler_u005b_2_u005d, _e22);
                    phi_76_ = _e30;
                    break;
                }
                case 3: {
                    let _e31 = textureSample(msdf_u005b_0_u005d, msdfSampler_u005b_3_u005d, _e22);
                    phi_76_ = _e31;
                    break;
                }
                default: {
                    phi_76_ = vec4<f32>();
                    break;
                }
            }
            let _e33 = phi_76_;
            phi_146_ = _e33;
            break;
        }
        case 1: {
            switch bitcast<i32>(_e25) {
                case 0: {
                    let _e35 = textureSample(msdf_u005b_1_u005d, msdfSampler_u005b_0_u005d, _e22);
                    phi_99_ = _e35;
                    break;
                }
                case 1: {
                    let _e36 = textureSample(msdf_u005b_1_u005d, msdfSampler_u005b_1_u005d, _e22);
                    phi_99_ = _e36;
                    break;
                }
                case 2: {
                    let _e37 = textureSample(msdf_u005b_1_u005d, msdfSampler_u005b_2_u005d, _e22);
                    phi_99_ = _e37;
                    break;
                }
                case 3: {
                    let _e38 = textureSample(msdf_u005b_1_u005d, msdfSampler_u005b_3_u005d, _e22);
                    phi_99_ = _e38;
                    break;
                }
                default: {
                    phi_99_ = vec4<f32>();
                    break;
                }
            }
            let _e40 = phi_99_;
            phi_146_ = _e40;
            break;
        }
        case 2: {
            switch bitcast<i32>(_e25) {
                case 0: {
                    let _e42 = textureSample(msdf_u005b_2_u005d, msdfSampler_u005b_0_u005d, _e22);
                    phi_122_ = _e42;
                    break;
                }
                case 1: {
                    let _e43 = textureSample(msdf_u005b_2_u005d, msdfSampler_u005b_1_u005d, _e22);
                    phi_122_ = _e43;
                    break;
                }
                case 2: {
                    let _e44 = textureSample(msdf_u005b_2_u005d, msdfSampler_u005b_2_u005d, _e22);
                    phi_122_ = _e44;
                    break;
                }
                case 3: {
                    let _e45 = textureSample(msdf_u005b_2_u005d, msdfSampler_u005b_3_u005d, _e22);
                    phi_122_ = _e45;
                    break;
                }
                default: {
                    phi_122_ = vec4<f32>();
                    break;
                }
            }
            let _e47 = phi_122_;
            phi_146_ = _e47;
            break;
        }
        case 3: {
            switch bitcast<i32>(_e25) {
                case 0: {
                    let _e49 = textureSample(msdf_u005b_3_u005d, msdfSampler_u005b_0_u005d, _e22);
                    phi_145_ = _e49;
                    break;
                }
                case 1: {
                    let _e50 = textureSample(msdf_u005b_3_u005d, msdfSampler_u005b_1_u005d, _e22);
                    phi_145_ = _e50;
                    break;
                }
                case 2: {
                    let _e51 = textureSample(msdf_u005b_3_u005d, msdfSampler_u005b_2_u005d, _e22);
                    phi_145_ = _e51;
                    break;
                }
                case 3: {
                    let _e52 = textureSample(msdf_u005b_3_u005d, msdfSampler_u005b_3_u005d, _e22);
                    phi_145_ = _e52;
                    break;
                }
                default: {
                    phi_145_ = vec4<f32>();
                    break;
                }
            }
            let _e54 = phi_145_;
            phi_146_ = _e54;
            break;
        }
        default: {
            phi_146_ = vec4<f32>();
            break;
        }
    }
    let _e56 = phi_146_;
    switch bitcast<i32>(_e25) {
        case 0: {
            let _e65 = textureDimensions(msdf_u005b_0_u005d, 0i);
            phi_168_ = _e65;
            break;
        }
        case 1: {
            let _e66 = textureDimensions(msdf_u005b_1_u005d, 0i);
            phi_168_ = _e66;
            break;
        }
        case 2: {
            let _e67 = textureDimensions(msdf_u005b_2_u005d, 0i);
            phi_168_ = _e67;
            break;
        }
        case 3: {
            let _e68 = textureDimensions(msdf_u005b_3_u005d, 0i);
            phi_168_ = _e68;
            break;
        }
        default: {
            phi_168_ = vec2<u32>();
            break;
        }
    }
    let _e70 = phi_168_;
    let _e77 = fwidth(_e22);
    out_u002e_var_u002e_SV_Target0_ = mix(vec4<f32>(0f, 0f, 0f, 0f), _e23, vec4(clamp(((max((0.5f * dot((vec2(_e24) / vec2<f32>(f32(_e70.x), f32())).xx, (vec2<f32>(1f, 1f) / _e77).xx)), 1f) * (max(min(_e56.x, _e56.y), min(max(_e56.x, _e56.y), _e56.z)) - 0.5f)) + 0.5f), 0f, 1f)));
    return;
}

@fragment 
fn main(@location(0) in_u002e_var_u002e_TEXCOORD0_: vec2<f32>, @location(1) in_u002e_var_u002e_TEXCOORD1_: vec4<f32>, @location(2) in_u002e_var_u002e_TEXCOORD2_: f32, @location(3) @interpolate(flat) in_u002e_var_u002e_TEXCOORD3_: u32) -> @location(0) vec4<f32> {
    in_u002e_var_u002e_TEXCOORD0_1 = in_u002e_var_u002e_TEXCOORD0_;
    in_u002e_var_u002e_TEXCOORD1_1 = in_u002e_var_u002e_TEXCOORD1_;
    in_u002e_var_u002e_TEXCOORD2_1 = in_u002e_var_u002e_TEXCOORD2_;
    in_u002e_var_u002e_TEXCOORD3_1 = in_u002e_var_u002e_TEXCOORD3_;
    main_1();
    let _e9 = out_u002e_var_u002e_SV_Target0_;
    return _e9;
}
