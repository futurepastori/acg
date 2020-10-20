uniform vec3 u_camera_pos;

varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;

uniform samplerCube u_texture;

void main() {
    vec3 ray_in = normalize(u_camera_pos - v_world_position);
    vec3 ray_out = reflect(ray_in, normalize(v_normal));

	gl_FragColor = vec4(texture(u_texture, ray_out).xyz, 1.0);
}
