varying vec3 v_tex_coords;
uniform samplerCube u_texture;

void main() {
	gl_FragColor = texture(u_texture, v_tex_coords);
}
