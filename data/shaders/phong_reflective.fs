varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec3 v_normal_refl;
varying vec2 v_uv;
varying vec4 v_color;

// uniforms
uniform vec3 diffuse_k;
uniform vec3 diffuse_i;

uniform float alpha;
uniform vec3 specular_k;
uniform vec3 specular_i;

uniform vec3 ambient_k;
uniform vec3 ambient_i;

uniform vec3 light_position;
uniform vec3 u_camera_position;

uniform samplerCube u_texture;

uniform vec4 u_color;


void main()
{
	vec2 uv = v_uv;
	//here we set up the normal as a color to see them as a debug

	//here write the computations for PHONG.
	//for GOURAUD you dont need to do anything here, just pass the color from the vertex shader
	vec3 N = normalize(v_normal);
	vec3 L = normalize( light_position - v_world_position);
	vec3 E = normalize( u_camera_position - v_world_position);
	vec3 R = normalize(reflect(E, N));

	float NdotL = max(0.0, dot(N,L));
	float RdotL = pow(max(0.0, dot(-R,L)), alpha);

	vec3 color = ambient_k * ambient_i + diffuse_k * diffuse_i * NdotL + specular_k * specular_i * RdotL;
	
	vec3 ray_in = normalize(u_camera_position - v_world_position);
    vec3 ray_out = reflect(-ray_in, normalize(-v_normal));

	//set the ouput color por the pixel
	gl_FragColor = vec4( color, 1.0 ) * vec4(texture(u_texture, ray_out).xyz, 1.0);
}