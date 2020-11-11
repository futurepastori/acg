// Constants
#define CLAMP_MAX 0.99
#define CLAMP_MIN 0.01
#define PI 3.14159265359
#define GAMMA 2.2
#define INV_GAMMA 0.45

varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;

uniform vec3 u_camera_position;
uniform vec4 u_color;

uniform float u_roughness_factor;
uniform float u_metalness_factor;
uniform vec3 u_light_position;

// Textures
uniform sampler2D u_albedo_map;
uniform sampler2D u_metalness_map;
uniform sampler2D u_roughness_map;
uniform sampler2D u_brdf_lut;

uniform sampler2D u_normal_map;

uniform bool u_with_opacity_map;
uniform bool u_with_occlusion_map;

uniform sampler2D u_opacity_map;
uniform sampler2D u_occlusion_map;

// Levels of the HDR Environment to simulate roughness material (IBL)
uniform samplerCube u_texture;		 	// Original 
uniform samplerCube u_texture_prem_0; 	// Level 0
uniform samplerCube u_texture_prem_1; 	// ..
uniform samplerCube u_texture_prem_2;	// ..
uniform samplerCube u_texture_prem_3;	// ..
uniform samplerCube u_texture_prem_4; 	// Level 5: Less reflective

struct PBRMat
{
	vec3 c_diffuse;
	vec3 f0_specular;

	vec3 f_diffuse;
	vec3 f_specular;

	float roughness;
	float metalness;
	vec3 albedo; // Base color

	float opacity;
	vec3 occlusion;

	vec3 DL;
	vec3 IBL;
	vec3 final_color;
};

struct PBRVec
{
	vec3 N;
	vec3 L;
	vec3 V;
	vec3 R;
	vec3 H;

	float n_dot_l;
	float n_dot_v;
	float n_dot_h;
	float l_dot_h;
};

// Convert 0-Inf range to 0-1 range so we can
// display info on screen

vec3 toneMap(vec3 color)
{
    return color / (color + vec3(1.0));
}

// degamma
vec3 gamma_to_linear(vec3 color)
{
	return pow(color, vec3(GAMMA));
}

// gamma
vec3 linear_to_gamma(vec3 color)
{
	return pow(color, vec3(INV_GAMMA));
}

//Javi Agenjo Snipet for Bump Mapping
mat3 cotangent_frame(vec3 N, vec3 p, vec2 uv){
	// get edge vectors of the pixel triangle
	vec3 dp1 = dFdx( p );
	vec3 dp2 = dFdy( p );
	vec2 duv1 = dFdx( uv );
	vec2 duv2 = dFdy( uv );

	// solve the linear system
	vec3 dp2perp = cross( dp2, N );
	vec3 dp1perp = cross( N, dp1 );
	vec3 T = dp2perp * duv1.x + dp1perp * duv2.x;
	vec3 B = dp2perp * duv1.y + dp1perp * duv2.y;

	// construct a scale-invariant frame
	float invmax = inversesqrt( max( dot(T,T), dot(B,B) ) );
	return mat3( T * invmax, B * invmax, N );
}

vec3 perturbNormal( vec3 N, vec3 V, vec2 texcoord, vec3 normal_pixel )
{
	normal_pixel = normal_pixel * 255./127. - 128./127.;
	mat3 TBN = cotangent_frame(N, V, texcoord);
	return normalize(TBN * normal_pixel);
}

void initMaterialVectors(out PBRVec vectors)
{
	// We initialize the normal as before and create a normal mapp
	// from the roughness texture
	vec3 N = normalize(v_normal);
	// L : vector towards the light
	vec3 L = normalize(u_light_position - v_world_position);
	// V: vector towards the eye 
	vec3 V = normalize(u_camera_position - v_world_position);
	// R: reflected L vector
	vec3 R = normalize(reflect(-V, N));
	// H: half vector between V and L
	vec3 H = normalize(V + L);

	vec4 normal_map = texture2D(u_normal_map, v_uv);
	
	vectors.N = perturbNormal(N, V, v_uv, normal_map.xyz);
	vectors.L = L;
	vectors.V = V;
	vectors.R = R;
	vectors.H = H;

	// Some necessary dot products for the BDRF equations
	vectors.n_dot_h = clamp(dot(vectors.N, vectors.H), CLAMP_MIN, CLAMP_MAX);
	vectors.n_dot_l = clamp(dot(vectors.N, vectors.L), CLAMP_MIN, CLAMP_MAX);
	vectors.n_dot_v = clamp(dot(vectors.N, vectors.V), CLAMP_MIN, CLAMP_MAX);
	vectors.l_dot_h = clamp(dot(vectors.L, vectors.H), CLAMP_MIN, CLAMP_MAX);
}

void initMaterialProps(out PBRMat material)
{

	// roguhness: facet deviation at the surface of the material
	vec4 material_roughness = texture2D(u_roughness_map, v_uv);
	material.roughness = material_roughness.x * u_roughness_factor;

	// Compute metalness
	vec4 metalness_texture = texture2D(u_metalness_map, v_uv);
	material.metalness = metalness_texture.x * u_metalness_factor;

	// Compute albedo (base color)
	vec4 albedo_texture = texture2D(u_albedo_map, v_uv);
	material.albedo = gamma_to_linear(albedo_texture.xyz);// Degamma before operation

	// Compute opacity map
	if (u_with_opacity_map) {
		vec4 opacity_texture = texture2D(u_opacity_map, v_uv);
		material.opacity = opacity_texture.x;
	} else {
		material.opacity = 1.0;
	}

	if (u_with_occlusion_map) {
		vec4 occlusion_texture = texture2D(u_occlusion_map, v_uv);
		material.occlusion = occlusion_texture.xyz;
	} else {
		material.occlusion = vec3(1.0);
	}

	// c_diffuse: base RGB diffuse color for Lambertian model
	material.c_diffuse = (material.metalness * vec3(0.0)) + ((1 - material.metalness) * material.albedo);

	// f0_specular: computed RGB color for the specular reflection
	material.f0_specular = (material.metalness * material.albedo) + ((1 - material.metalness) * vec3(0.04));
}

// *** Direct Lighting ***
vec3 getFresnel(PBRMat material, PBRVec vectors)
{
	// return: RGB color for the Fresnel reflection equation
	return material.f0_specular + (1.0 - material.f0_specular) * pow((1.0 - vectors.l_dot_h), 5.0);
}

float getGeometry(PBRMat material, PBRVec vectors)
{
	// k: geometry function factor
	float k = pow((material.roughness + 1.0), 2.0) / 8.0;

	// G1, G2: geometry factors for occlusion given L and V
	float G1 = vectors.n_dot_l/(vectors.n_dot_l * (1.0-k)+k);
	float G2 = vectors.n_dot_v/(vectors.n_dot_v * (1.0-k)+k);

	return G1*G2;
}

float getDistribution(PBRMat material, PBRVec vectors)
{
	// alpha_pow2: distribution amplitude parameter
	float alpha_pow2 = pow(material.roughness, 2.0);
	// density: pointwise color density given underlying distribution
	float density = pow(pow(vectors.n_dot_h, 2.0) * (alpha_pow2-1.0) + 1, 2.0);
	
	return alpha_pow2 / density;
}

void setDirectLighting(out PBRMat material, out PBRVec vectors)
{
	material.f_diffuse = material.c_diffuse / PI;

	// F: color for the Fresnel reflection
	vec3 F = getFresnel(material, vectors);
	// G: amount of this color to be present by geometry
	float G = getGeometry(material, vectors);
	// D: pointwise density of this color given distribution fn
	float D = getDistribution(material, vectors);

	// f_specular: specular color, f_facet refelction equation
	material.f_specular = (F*G*D) / (4*vectors.n_dot_l*vectors.n_dot_v);
	// combining diffuse and specular direct lighting
	material.DL = material.f_diffuse + material.f_specular;
}

// *** Image Based Lighting ***

// Get the corresponding environment reflection given the 
// material roughness and the reflection vector
vec3 getReflectionColor(vec3 r, float roughness)
{
	float lod = roughness * 5.0;
	vec4 color;

	if(lod < 1.0) color = mix( textureCube(u_texture, r), textureCube(u_texture_prem_0, r), lod );
	else if(lod < 2.0) color = mix( textureCube(u_texture_prem_0, r), textureCube(u_texture_prem_1, r), lod - 1.0 );
	else if(lod < 3.0) color = mix( textureCube(u_texture_prem_1, r), textureCube(u_texture_prem_2, r), lod - 2.0 );
	else if(lod < 4.0) color = mix( textureCube(u_texture_prem_2, r), textureCube(u_texture_prem_3, r), lod - 3.0 );
	else if(lod < 5.0) color = mix( textureCube(u_texture_prem_3, r), textureCube(u_texture_prem_4, r), lod - 4.0 );
	else color = textureCube(u_texture_prem_4, r);

	return color.rgb;
}

vec4 getBRDFLUT(PBRMat material, PBRVec vectors)
{
	vec2 vector = vec2(vectors.n_dot_v, material.roughness);
	return texture2D(u_brdf_lut, vector);
}

void setIndirectLighting(out PBRMat material, out PBRVec vectors)
{
	vec4 BRDF_LUT = getBRDFLUT(material, vectors);

	// diffuse IBL as the interpolation between a sample of the reflected color
	// from the environment and the underlying diffuse color (L2 slides, pp. 43)
	vec3 diffuse_sample = getReflectionColor(vectors.N, 1.0);
	vec3 diffuse_color = material.c_diffuse;
	vec3 diffuse_IBL = diffuse_sample * diffuse_color;

	// specular IBL is taken from the sampling of the reflection value
	// and the coordinates from a LUT (L2 slides, pp. 45) - brdf light terms
	vec3 specular_sample = getReflectionColor(vectors.R, material.roughness);
	vec3 specular_BDRF = material.f0_specular * BRDF_LUT.x + BRDF_LUT.y;
	vec3 specular_IBL = specular_sample * specular_BDRF;

	// environment color
	material.IBL = (diffuse_IBL + specular_IBL) * material.occlusion;
}

// *** Final Color ***
void getPixelColor(out PBRMat material, out PBRVec vectors)
{
	setDirectLighting(material, vectors);
	setIndirectLighting(material, vectors);
	material.final_color = material.DL + material.IBL;
}


void main()
{
	PBRMat pbr_material;
	PBRVec pbr_vectors;

	initMaterialVectors(pbr_vectors);
	initMaterialProps(pbr_material);

	getPixelColor(pbr_material, pbr_vectors);

	// Total color
	vec4 color = vec4(pbr_material.final_color, 1.0);

	// Tone mapping for the HDR envoronments
	color.xyz = toneMap(color.xyz);
	//Gamma correction to show in screen
	color.xyz = linear_to_gamma(color.xyz);
	
	//vec4 test_color = textureCube(u_texture_prem_4, pbr_vectors.R);
	//test_color.xyz = toneMap(test_color.xyz);
	
	gl_FragColor = vec4(color.x, color.y, color.z, pbr_material.opacity);
}