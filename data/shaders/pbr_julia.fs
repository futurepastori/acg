#define PI 3.14159265359
#define RECIPROCAL_PI 0.3183098861837697

#define GAMMA 2.2
#define INV_GAMMA 0.45

varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;

uniform vec3 u_camera_position;
uniform vec4 u_color;

// Levels of the HDR Environment to simulate roughness material
// (IBL)
//uniform samplerCube u_texture_prem_0;
//uniform samplerCube u_texture_prem_1;
//uniform samplerCube u_texture_prem_2;
//uniform samplerCube u_texture_prem_3;
//uniform samplerCube u_texture_prem_4;

uniform float u_roughness;
uniform float u_metalness;
uniform vec3 u_light_position;

// maps
uniform sampler2D u_roughness_map;
uniform sampler2D u_metalness_map;
uniform sampler2D u_albedo_map;
//uniform sampler2D u_brdf;

struct PBRMat
{
	vec3 N;
	vec3 L;
	vec3 V;
	vec3 R;
	vec3 H;
	
	vec3 c_diff;
	vec3 f0_specular;
	float roughness;
	float metalness;
	vec3 albedo; // Base color

	vec3 f_diffuse;
	vec3 f_specular;

	vec3 direct_lighting;
	vec3 final_color;
};

vec3 toneMap(vec3 color)
{
    return color / (color + vec3(1.0));
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

vec3 perturbNormal( vec3 N, vec3 V, vec2 texcoord, vec3 normal_pixel ){
	#ifdef USE_POINTS
	return N;
	#endif

	// assume N, the interpolated vertex normal and
	// V, the view vector (vertex to eye)
	//vec3 normal_pixel = texture2D(normalmap, texcoord ).xyz;
	normal_pixel = normal_pixel * 255./127. - 128./127.;
	mat3 TBN = cotangent_frame(N, V, texcoord);
	return normalize(TBN * normal_pixel);
}

void lightEquationVectors(out PBRMat material)
{
	// N : normal vector at each point
	vec3 N_aux = normalize(v_normal);// TODO: this has to be perturbnormal (microfacets)
	vec4 normal_map = texture2D(u_roughness_map, v_uv);

	// L : vector towards the light
	material.L = normalize(u_light_position - v_world_position);
	// V: vector towards the eye 
	material.V = normalize(u_camera_position - v_world_position);
	// R: reflected L vector
	material.R = reflect(material.V, material.N);
	// H: half vector between V and L
	material.H = material.V + material.L;

	// Microfacets in all the surface - irregular surface
	// Computation of perturbed normal
	material.N = perturbNormal( N_aux, material.V, v_uv, normal_map.rgb);
}

void assignMaterialVal(out PBRMat material){

	// Compute roughness
	vec4 roughness_texture = texture2D(u_roughness_map, v_uv);
	material.roughness = roughness_texture.r * u_roughness;
	
	// Compute metalness
	vec4 metalness_texture = texture2D(u_metalness_map, v_uv);
	material.metalness = metalness_texture.r * u_metalness;

	// Compute albedo (base color)
	vec4 albedo_texture = texture2D(u_albedo_map, v_uv);
	material.albedo = albedo_texture.rgb;

	// Compute diffuse colour / diffuse albedo
	material.c_diff = (material.metalness * vec3(0.0)) + ((1 - material.metalness) * material.albedo);
	// Compute specular colour / specular albedo
	material.f0_specular = (material.metalness * material.albedo) + ((1 - material.metalness) * vec3(0.04));
	
}

// DIRECT LIGHTING

vec3 fresnel(PBRMat material)
{
	// Compute Fresnet Reflective F
	float l_dot_n = clamp(dot(material.L, material.N), 0.01, 0.99);
	return material.f0_specular + (1.0 - material.f0_specular) * pow((1.0 - l_dot_n), 5.0);
}

float G1(float dot_vec, float k)
{
	return dot_vec/(dot_vec * (1.0 - k) + k);
}

float geometry_function(PBRMat material)
{
	// Compute Geometry Function G
	float k = pow((material.roughness + 1.0), 2.0) / 8.0;
	float n_dot_l = clamp(dot(material.N, material.L), 0.01, 0.99);
	float n_dot_v = clamp(dot(material.N, material.V), 0.01, 0.99);
	return G1(n_dot_l,k) * G1(n_dot_v,k);
}

float dist_function(PBRMat material){

	// Compute Distribution Function D
	float alpha = pow(material.roughness, 2);
	float alpha_pow2 = pow(alpha, 2);

	float n_dot_m = clamp(dot(material.N, material.H), 0.01, 0.99);
	return alpha_pow2/(PI * (pow(n_dot_m,2) * (alpha_pow2 - 1.0) + 1.0), 2.0);
}

void directLighting(out PBRMat material)
{
	float n_dot_l = clamp(dot(material.N, material.L), 0.01, 0.99);
	float n_dot_v = clamp(dot(material.N, material.V), 0.01, 0.99);

	//Diffuse term (Lambert)
	material.f_diffuse = material.c_diff / PI;
	
	//Specular term (facet)
	vec3 F = fresnel(material);
	float G = geometry_function(material);
	float D = dist_function(material);
	material.f_specular = (F * G * D) / (4 * n_dot_l * n_dot_v);

	//PL color:
	material.direct_lighting = material.f_diffuse + material.f_specular;
}

void computeFinalColor(out PBRMat material)
{
	
	// Direct lighting
	directLighting(material);

	// Compute final color (combination between direct & lighting and IBL (todo))
	material.final_color = material.direct_lighting;
}

void main()
{
	// Define PBR material
	PBRMat pbr_material;

	// Compute light equation vectors
	lightEquationVectors(pbr_material);
	
	// Assign material values
	assignMaterialVal(pbr_material);

	// Compute final color
	computeFinalColor(pbr_material);

	vec4 color = vec4(pbr_material.final_color, 1.0);

	gl_FragColor = color;
}