varying vec3 v_tex_coords;
uniform samplerCube u_texture;

void main() {
	gl_FragColor = texture(u_texture, v_tex_coords);
}

/* varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;
varying vec4 v_color;

uniform vec4 u_color;
uniform sampler2D u_texture;
uniform float u_time;

void main()
{
	vec2 uv = v_uv;
	gl_FragColor = u_color * texture2D( u_texture, uv );
}
 */