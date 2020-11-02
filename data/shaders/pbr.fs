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

// comentari de mostra

uniform vec3 pos_light;

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

	vec3 f_diffuse;
	vec3 f_specular;

	vec3 bsdf;
	vec3 final_color;
};

vec3 toneMap(vec3 color)
{
    return color / (color + vec3(1.0));
}

void lightEquationVectors(out PBRMat material)
{
	// N : normal vector at each point
	material.N = normalize(v_normal);// TODO: this has to be perturbnormal (microfacets)
	// L : vector towards the light
	material.L = normalize(pos_light - v_world_position);
	// V: vector towards the eye 
	material.V = normalize(u_camera_position - v_world_position);
	// R: reflected L vector
	material.R = reflect(material.V, material.N);
	// H: half vector between V and L
	material.H = material.V + material.L;
}

PBRMat assignMaterialVal(PBRMat material){

	material.roughness = 0.5;
	material.c_diff = vec3(0.5, 0.5, 1.0);
	//Compute F0 specular
	material.f0_specular = vec3(0.5, 0.5, 1.0);
	return material;
}

// DIRECT LIGHTING

vec3 fresnel(PBRMat material)
{
	// Compute Fresnet Reflective F
	float l_dot_n = clamp(dot(material.L, material.N), 0.01, 0.99);
	return material.f0_specular + (1 - material.f0_specular) * pow((1 - l_dot_n),5);
}

float G1(float dot_vec, float k)
{
	return dot_vec/(dot_vec * (1 - k) + k);
}

float geometry_function(PBRMat material)
{

	// Compute Geometry Function G
	float k = pow((material.roughness + 1), 2)/8;
	float n_dot_l = clamp(dot(material.N, material.L), 0, 1);
	float n_dot_v = clamp(dot(material.N, material.V), 0, 1);

	return G1(n_dot_l,k) * G1(n_dot_v,k);
}

float dist_function(PBRMat material){

	// Compute Distribution Function D
	float alpha = pow(material.roughness, 2);
	float alpha_pow2 = pow(alpha, 2);

	float n_dot_m = clamp(dot(material.N, material.H), 0, 1);
	return alpha_pow2/(PI * (pow(n_dot_m,2) * (alpha_pow2 - 1) + 1), 2);
}

PBRMat directLighting(PBRMat material)
{
	float n_dot_l = clamp(dot(material.N, material.L), 0, 1);
	float n_dot_v = clamp(dot(material.N, material.V), 0, 1);

	//Diffuse term (Lambert)
	material.f_diffuse = material.c_diff / PI;
	
	//Specular term (facet)
	vec3 F = fresnel(material);
	float G = geometry_function(material);
	float D = dist_function(material);
	material.f_specular = (F * G * D) / (4 * n_dot_l * n_dot_v);

	//PL color:
	material.bsdf = material.f_diffuse + material.f_specular;

	return material;
}

PBRMat computeFinalColor(PBRMat material)
{
	material.final_color = material.bsdf;
	return material;
}

void main()
{
	// Define PBR material
	PBRMat pbr_material;

	// Compute light equation vectors
	lightEquationVectors(pbr_material);
	
	// Assign material values
	pbr_material = assignMaterialVal(pbr_material);

	// Direct lighting
	pbr_material = directLighting(pbr_material);

	// Compute final color
	pbr_material = computeFinalColor(pbr_material);

	vec4 color = vec4(pbr_material.final_color, 1.0);

	gl_FragColor = color;
}