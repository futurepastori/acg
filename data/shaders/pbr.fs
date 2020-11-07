#define CLAMP_MAX 0.99
#define CLAMP_MIN 0.01

#define PI 3.14159265359

varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;

uniform vec3 u_camera_position;
uniform vec4 u_color;
uniform vec3 pos_light;


struct PBRMat
{
	vec3 c_diffuse;
	vec3 f0_specular;
	float roughness;

	vec3 f_diffuse;
	vec3 f_specular;

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

void initMaterialVectors(out PBRVec vectors)
{
	// N : normal vector at each point
	vectors.N = normalize(v_normal); // TODO: this has to be perturbnormal (microfacets)
	// L : vector towards the light
	vectors.L = normalize(pos_light - v_world_position);
	// V: vector towards the eye 
	vectors.V = normalize(u_camera_position - v_world_position);
	// R: reflected L vector
	vectors.R = normalize(reflect(vectors.V, vectors.N));
	// H: half vector between V and L
	vectors.H = normalize(vectors.V + vectors.L);

	// Some necessary dot products for the BDRF equations
	vectors.n_dot_h = clamp(dot(vectors.N, vectors.H), CLAMP_MIN, CLAMP_MAX);
	vectors.n_dot_l = clamp(dot(vectors.N, vectors.L), CLAMP_MIN, CLAMP_MAX);
	vectors.n_dot_v = clamp(dot(vectors.N, vectors.V), CLAMP_MIN, CLAMP_MAX);
	vectors.l_dot_h = clamp(dot(vectors.L, vectors.H), CLAMP_MIN, CLAMP_MAX);
}

void initMaterialProps(out PBRMat material)
{
	// roguhness: facet deviation at the surface of the material
	material.roughness = 0.85;
	// c_diffuse: base RGB diffuse color for Lambertian model
	material.c_diffuse = vec3(0.35, 0.35, 0.55);
	// f0_specular: computed RGB color for the specular reflection
	material.f0_specular = vec3(0.7, 0.8, 0.95);	// TODO: this is still hardcoded
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