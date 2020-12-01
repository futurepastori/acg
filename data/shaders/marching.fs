#define MAX_STEPS 250
#define PLANE_LIMIT 1.0

varying vec3 v_position;
varying vec3 v_world_position;

uniform vec3 u_camera_position;

uniform bool apply_jittering;
uniform float u_ray_step;
uniform float u_threshold;

uniform vec4 u_color;
uniform mat4 u_model;

// Texture is in 3D
// TODO: We will also have to sample for a color texture map,
// which will be a regular sampler2D. For now, we return flat.
uniform sampler3D u_texture;
uniform sampler2D u_noise_texture;
uniform sampler2D u_transfer_function;

// Skip coordinates (where )
uniform float u_clip_plane_x;
uniform float u_clip_plane_y;
uniform float u_clip_plane_z;


void main()
{
	//0. DUMMY VARIABLES
	vec3 clip_plane = vec3(u_clip_plane_x, u_clip_plane_y, u_clip_plane_z);
	
	// 1. RAY SETUP
	vec3 current_sample = v_position; // first sample pos

	vec3 ray_origin = u_camera_position; // ray origin

	vec3 ray_dir = normalize(current_sample - ray_origin); // ray direction
	vec3 step_vec = ray_dir * u_ray_step; // Step vector

	// 1.5 JITTERING
	if(apply_jittering){
		vec2 frag_coord = gl_FragCoord.xy; // getting coordinates from fragment instead of sampling from vec
		float jittering = texture2D(u_noise_texture, frag_coord).x;	// getting only one channel as float
		current_sample += step_vec * jittering;	// modulate the step vector by the deviation of the jittering
	}
	
	float step_length = length(step_vec);
	vec4 final_color = vec4(0.0, 0.0, 0.0, 0.0);
	
	for (int i=1; i < MAX_STEPS; i++)
	{
		if (current_sample.x < clip_plane.x) break;
		if (current_sample.y < clip_plane.y) break;
		if (current_sample.z < clip_plane.z) break;

		// 2. VOLUME SAMPLING
		float density = texture3D(u_texture, (current_sample+1)/2).r;

		// 3. CLASSIFICATION
		vec4 sample_color = texture2D(u_transfer_function, vec2(2*density, 1.0));
		//vec4 sample_color = vec4(0.0);
		
		//if (density < 0.02) {
		//	sample_color = vec4(0.0);
		//} else if (density < 0.3) {
		//	sample_color = vec4(1.0, 0.0, 0.0, 0.03);
		//} else if (density < 0.5) {
		//	sample_color = vec4(0.0, 1.0, 0.0, 0.55);
		//} else {
		//	sample_color = vec4(1.0, 1.0, 1.0, 1.0);
		//}

		//sample_color = vec4(1.0, 1.0, 1.0, 0.88);


		//if(density > u_threshold){
			
			// GRADIENT
			//vec3 pos_x = (current_sample.x + step_vec, current_sample.y, current_sample.z);
			//vec3 neg_x = (current_sample.x - step_vec, current_sample.y, current_sample.z);
			//float grad_x = (texture3D(u_texture, pos_x) - texture3D(u_texture, neg_x)) / (2*step_vec);

			//vec3 pos_y = (current_sample.x, current_sample.y + step_vec, current_sample.z);
			//vec3 neg_y = (current_sample.x, current_sample.y - step_vec, current_sample.z);
			//float grad_y = (texture3D(u_texture, pos_y) - texture3D(u_texture, neg_y)) / (2*step_vec);

			//vec3 pos_z = (current_sample.x, current_sample.y, current_sample.z + step_vec);
			//vec3 neg_z = (current_sample.x, current_sample.y, current_sample.z - step_vec);
			//float grad_z = (texture3D(u_texture, pos_z) - texture3D(u_texture, neg_z)) / (2*step_vec);

			//vec3 gradient = (grad_x, grad_y, grad_z);

			// After this step, alpha is 1
			//final_color.a = 1;

			//TODO: compute sample color to shade the surface
		}

		// 4. COMPOSITION
		final_color += step_length * (1-final_color.a) * sample_color;

		// 5. NEXT SAMPLE
		current_sample += step_vec;

		// EARLY TERMINATION
		// Are we checking outside the limits of the volume's planes?
		// Remember that we are in a normalized space in [0, 1]
		if (current_sample.x > PLANE_LIMIT || current_sample.x < -PLANE_LIMIT) break;		
		if (current_sample.y > PLANE_LIMIT || current_sample.y < -PLANE_LIMIT) break;		
		if (current_sample.z > PLANE_LIMIT || current_sample.z < -PLANE_LIMIT) break;
		// when final color alpha reaches 1 --> the color will not change anymore.
		if (final_color.a >= 1) break;
	}

	gl_FragColor = final_color;
}
