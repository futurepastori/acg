#version 140

attribute vec3 a_vertex;
attribute vec3 a_normal;
attribute vec2 a_uv;
attribute vec4 a_color;

uniform vec3 u_camera_pos;

uniform mat4 u_model;
uniform mat4 u_viewprojection;

varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;
varying vec4 v_color;

void main() {
    v_normal = mat3(transpose(inverse(u_model))) * a_normal;
    v_position = vec3(u_model * vec4(a_vertex, 1.0));

    gl_Position = u_viewprojection * vec4(v_position, 1.0);
}