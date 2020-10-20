attribute vec3 a_vertex;

uniform mat4 u_view;
uniform mat4 u_projection;

varying vec3 v_tex_coords;

void main() {
	v_tex_coords = a_vertex;
	gl_Position = u_projection * u_view * vec4(a_vertex, 1.0);
}
