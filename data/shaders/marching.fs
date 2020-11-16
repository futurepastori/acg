#define MAX_STEPS 100
#define PLANE_LIMIT 1.0

varying vec3 v_position;

uniform vec3 u_camera_position;
uniform vec3 u_ray_origin;
uniform float step_modul;

uniform vec4 u_color;
// Texture is in 3D
// TODO: We will also have to sample for a color texture map,
// which will be a regular sampler2D. For now, we return flat.
uniform sampler3D u_texture;


void main()
{
	// 1. RAY SETUP
	vec3 current_sample = v_position; // first sample pos
	vec3 eye_position = u_camera_position; // ray origin

	vec3 ray = normalize(current_sample - u_camera_position); // ray direction
	vec3 step_vec = ray * step_modul; // Step vector

	vec4 final_color = vec4(0.0);

	for (int i=1; i < MAX_STEPS; i++)
	{
		// 2. VOLUME SAMPLING
		current_sample = clamp(current_sample, 0.01, 0.99);
		float tex_value = texture3D(u_texture, current_sample).x;

		// 3. CLASSIFICATION
		// TODO: applies density function
		vec4 sample_color = vec4(tex_value);
		
		// 4. COMPOSITION
		float step_length = length(step_vec);
		final_color += step_length * (1 - sample_color.a) * sample_color;

		// 5. NEXT SAMPLE
		current_sample += step_vec;

		// EARLY TERMINATION
		// Are we checking outside the limits of the volume's planes?
		// Remember that we are in a normalized space in [0, 1]
		if (current_sample.x > PLANE_LIMIT) break;		
		if (current_sample.y > PLANE_LIMIT) break;		
		if (current_sample.z > PLANE_LIMIT) break;
		// when final color alpha reaches 1 --> the color will not change anymore.
		if (final_color.a >= 1) break;

	}

	gl_FragColor = final_color;
}
