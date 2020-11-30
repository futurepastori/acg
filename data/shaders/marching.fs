#define MAX_STEPS 100
#define PLANE_LIMIT 1.0

varying vec3 v_position;
varying vec3 v_world_position;

uniform vec3 u_camera_position;
uniform vec3 u_ray_origin;
uniform float u_ray_step;
uniform vec4 u_color;
uniform mat4 u_model;

// Texture is in 3D
// TODO: We will also have to sample for a color texture map,
// which will be a regular sampler2D. For now, we return flat.
uniform sampler3D u_texture;
uniform sampler2D u_noise_texture;
uniform sampler2D u_transfer_function;


void main()
{
	// 1. RAY SETUP
	vec3 current_sample = v_position; // first sample pos

	vec3 ray_origin = u_camera_position; // ray origin

	vec3 ray_dir = normalize(current_sample - ray_origin); // ray direction
	vec3 step_vec = ray_dir * u_ray_step; // Step vector

	// 1.5 JITTERING
	vec2 frag_coord = gl_FragCoord.xy; // getting coordinates from fragment instead of sampling from vec
	float jittering = texture2D(u_noise_texture, frag_coord).x;	// getting only one channel as float
	current_sample += step_vec * jittering;	// modulate the step vector by the deviation of the jittering
	
	vec4 final_color = vec4(0.0);

	for (int i=1; i < MAX_STEPS; i++)
	{
		// 2. VOLUME SAMPLING
		float density = texture3D(u_texture, (current_sample+1)/2).x;

		// 3. CLASSIFICATION
		vec4 sample_color = texture2D(u_transfer_function, vec2(density, 1.0));

		// 4. COMPOSITION
		float step_length = length(step_vec);
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
