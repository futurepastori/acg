#define MAX_STEPS 250
#define PLANE_LIMIT 1.0

varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_local_camera_position;

uniform vec3 u_camera_position;

uniform bool apply_jittering;
uniform float u_ray_step;
uniform float u_threshold;

uniform vec4 u_color;
uniform mat4 u_model;

uniform sampler3D u_texture;
uniform sampler2D u_noise_texture;
uniform sampler2D u_transfer_function;

uniform vec3 u_light_position;

// Skip coordinates (where )
uniform float u_clip_plane_x;
uniform float u_clip_plane_y;
uniform float u_clip_plane_z;

float jittering()
{
	vec2 frag_coord = gl_FragCoord.xy; // getting coordinates from fragment instead of sampling from vec
	float jittering = texture2D(u_noise_texture, frag_coord*0.01).x;// getting only one channel as float
	return jittering;
}

vec3 computeGradient( vec3 current_sample, float step_length )
{
	// GRADIENT
	vec3 pos_x = vec3(current_sample.x + step_length, current_sample.y, current_sample.z);
	vec3 neg_x = vec3(current_sample.x - step_length, current_sample.y, current_sample.z);
	float grad_x = (texture3D(u_texture, pos_x).r - texture3D(u_texture, neg_x).r) / (2*step_length);

	vec3 pos_y = vec3(current_sample.x, current_sample.y + step_length, current_sample.z);
	vec3 neg_y = vec3(current_sample.x, current_sample.y - step_length, current_sample.z);
	float grad_y = (texture3D(u_texture, pos_y).r - texture3D(u_texture, neg_y).r) / (2*step_length);

	vec3 pos_z = vec3(current_sample.x, current_sample.y, current_sample.z + step_length);
	vec3 neg_z = vec3(current_sample.x, current_sample.y, current_sample.z - step_length);
	float grad_z = (texture3D(u_texture, pos_z).r - texture3D(u_texture, neg_z).r) / (2*step_length);

	return vec3(grad_x, grad_y, grad_z);
}

void isoColor( inout vec4 final_color, vec4 sample_color, vec3 gradient )
{
	// Computes sample color to shade the isosurface
	vec3 N = gradient;
	// L : vector towards the light
	vec3 L = normalize(u_light_position - v_position);
			
	float NdotL = clamp(dot(N,L), 0.01, 0.99);
	sample_color.rgb = sample_color.rgb * NdotL;
	sample_color.rgb *= sample_color.a;
	final_color += sample_color * (1 - final_color.a);
}

bool earlyTermination( vec3 current_sample , vec4 final_color)
{
	// EARLY TERMINATION
	// Are we checking outside the limits of the volume's planes?
	// Remember that we are in a normalized space in [0, 1]
	if (current_sample.x > PLANE_LIMIT || current_sample.x < -PLANE_LIMIT) return true;		
	if (current_sample.y > PLANE_LIMIT || current_sample.y < -PLANE_LIMIT) return true;		
	if (current_sample.z > PLANE_LIMIT || current_sample.z < -PLANE_LIMIT) return true;
	// when final color alpha reaches 1 --> the color will not change anymore.
	if (final_color.a >= 1) return true;

	return false;
}

void main()
{
	//0. DUMMY VARIABLES
	// FIXME: Ajustar be valors
	vec4 clip_plane = vec4(0.0,8.0,0.0,-4.0);
	
	// 1. RAY SETUP
	vec3 current_sample = v_position; // first sample pos
	vec3 ray_origin = v_local_camera_position; // ray origin
	vec3 ray_dir = normalize(current_sample - ray_origin); // ray direction
	vec3 step_vec = ray_dir * u_ray_step; // Step vector

	// 1.5 JITTERING
	if(apply_jittering){
		current_sample += step_vec * jittering();	// modulate the step vector by the deviation of the jittering
	}
	
	float step_length = length(step_vec);
	vec4 final_color = vec4(0.0, 0.0, 0.0, 0.0);
	
	for (int i=1; i < MAX_STEPS; i++)
	{
		// VOLUME CLIPPING
		if ((clip_plane.x*current_sample.x + clip_plane.y*current_sample.y + clip_plane.z*current_sample.z + clip_plane.w) > 0.0) 
		{
			current_sample += step_vec;
			continue;
		}

		// 2. VOLUME SAMPLING
		float density = texture3D(u_texture, (current_sample+1.0)/2.0).r;

		// 3. CLASSIFICATION
		//vec4 sample_color = texture2D(u_transfer_function, vec2(density, 1.0));
		vec4 sample_color;
		if (density < 0.01) sample_color = vec4(0.0);
		else if (density < 0.24) sample_color = vec4(1.0, 0.0, 0.0, 0.3);
		else if (density < 0.31) sample_color = vec4(0.0, 1.0, 0.0, 0.3);
		else sample_color = vec4(1.0, 1.0, 1.0, 1.0);

		// 4. COMPOSITION
		sample_color.rgb *= sample_color.a;
		final_color += step_length * (1-final_color.a) * sample_color;

		if (density > u_threshold)
		{	
			vec3 gradient = computeGradient(current_sample, step_length);
			isoColor(final_color, sample_color, gradient);
			
			// After this step, alpha is 1
			final_color.a = 1;
		}

		// 5. NEXT SAMPLE
		current_sample += step_vec;

		// EARLY TERMINATION
		if (earlyTermination(current_sample, final_color)) break;

	}

	gl_FragColor = final_color*2;
}
