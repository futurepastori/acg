#define MAX_STEPS 50
#define PLANE_LIMIT 1.0

varying vec3 v_position;

uniform vec3 u_camera_position;
uniform vec3 u_ray_origin;

uniform vec4 u_color;
// Texture is in 3D
// TODO: We will also have to sample for a color texture map,
// which will be a regular sampler2D. For now, we return flat.
uniform sampler3D u_texture;


void main()
{
	vec3 tex_position = v_position;
	vec3 eye_position = u_camera_position;

	vec3 ray = normalize(tex_position - u_camera_position);
	vec3 ray_dt = ray*(1/MAX_STEPS);

	vec4 final_color = vec4(0.0);

	for (int i=1; i<MAX_STEPS; i++)
	{
		// Are we checking outside the limits of the volume's planes?
		// Remember that we are in a normalized space in [0, 1]
		if (tex_position.x > PLANE_LIMIT) break;		
		if (tex_position.y > PLANE_LIMIT) break;		
		if (tex_position.z > PLANE_LIMIT) break;

		float tex_value = texture3D(u_texture, tex_position).x;
		vec4 step_color = vec4(tex_value);
		
		final_color += step_color;

		tex_position += ray_dt;
		if (final_color.a >= 1) break;
	}

	gl_FragColor = final_color;
}
