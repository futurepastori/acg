#define CLAMP_MAX 0.99
#define CLAMP_MIN 0.01

#define PI 3.14159265359

varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;

uniform vec3 u_camera_position;
uniform vec4 u_color;

uniform float u_roughness_factor;
uniform float u_metalness_factor;
uniform vec3 u_light_position;

uniform sampler2D u_roughness_map;
uniform sampler2D u_metalness_map;
uniform sampler2D u_normal_map;
uniform sampler2D u_albedo_map;

struct PBRMat
{
	vec3 c_diffuse;
	vec3 f0_specular;

	vec3 f_diffuse;
	vec3 f_specular;

	float roughness;
	float metalness;
	vec3 albedo; // Base color

	vec3 bsdf;
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
	vec3 R = normalize(reflect(V, N));
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
	material.albedo = albedo_texture.xyz;

	// c_diffuse: base RGB diffuse color for Lambertian model
	material.c_diffuse = ((1 - material.metalness) * material.albedo);

	// f0_specular: computed RGB color for the specular reflection
	material.f0_specular = (material.metalness * material.albedo) + ((1 - material.metalness) * vec3(0.04));
}

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
	// normalized BSDF lighting combining diffuse and specular lighting
	material.bsdf = (material.f_diffuse + material.f_specular)*vectors.n_dot_l;
}

void main()
{
	PBRMat pbr_material;
	PBRVec pbr_vectors;

	initMaterialVectors(pbr_vectors);
	initMaterialProps(pbr_material);

	setDirectLighting(pbr_material, pbr_vectors);

	gl_FragColor = vec4(pbr_material.bsdf, 1.0);
}