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
uniform samplerCube u_texture_prem_0;
uniform samplerCube u_texture_prem_1;
uniform samplerCube u_texture_prem_2;
uniform samplerCube u_texture_prem_3;
uniform samplerCube u_texture_prem_4;

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

	// Gamma correction
	color = pow(color, vec4(INV_GAMMA));

	return color.rgb;
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


vec3 toneMap(vec3 color)
{
    return color / (color + vec3(1.0));
}

PBRMat lightEquationVectors(PBRMat material)
{
	// N : normal vector at each point
	material.N = normalize(v_normal);// TODO: this has to be perturbnormal (microfacets)
	// L : vector towards the light
	material.L = normalize(pos_light - v_world_position);
	// V: vector towards the eye 
	material.V = normalize(u_camera_position - v_world_position);
	// R: reflected L vector
	material.R = reflect(V, N);
	// H: half vector between V and L
	material.H = V + L;

	return material;
}

PBRMat assignMaterialVal(PBRMat material){

	material.roughness = 0.5;
	material.c_diff = vec3(0.5, 0.5, 1);
	//Compute F0 specular
	material.f0_specular = vec3(0.5, 0.5, 1);
	return material;
}

// DIRECT LIGHTING

vec3 fresnel(PBRMat material)
{
	// Compute Fresnet Reflective F
	float l_dot_n = clamp(dot(material.L, material.N), 0, 1);
	return material.f0_specular + (1 - material.f0_specular) * pow((1 - l_dot_n),5);
}

float G1(float dot, float k)
{
	return dot/(dot * (1 - k) + k)
}

float geometry_function(PBRMat material){

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
	pbr_material = lightEquationVectors(pbr_material);
	
	// Assign material values
	pbr_material = assignMaterialVal(pbr_material);

	// Direct lighting
	pbr_material = directLighting(pbr_material);

	// Compute final color
	pbr_material = computeFinalColor(pbr_material);

	vec4 color = vec4(PBR_material.final_color, 1.0);
	
	gl_FragColor = color;
}