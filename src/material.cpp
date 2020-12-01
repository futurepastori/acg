#include "material.h"
#include "texture.h"
#include "application.h"

StandardMaterial::StandardMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/flat.fs");
}

StandardMaterial::~StandardMaterial()
{

}

void StandardMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	//upload node uniforms
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_camera_position", camera->eye);
	shader->setUniform("u_model", model);
	shader->setUniform("u_time", Application::instance->time);

	shader->setUniform("u_color", color);

	if (texture)
		shader->setUniform("u_texture", texture);
}

void StandardMaterial::render(Mesh* mesh, Matrix44 model, Camera* camera)
{
	//set flags
	glEnable(GL_DEPTH_TEST);
	glDisable(GL_CULL_FACE);

	if (mesh && shader)
	{
		//enable shader
		shader->enable();

		//upload uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		//disable shader
		shader->disable();
	}
}

void StandardMaterial::renderInMenu()
{
	ImGui::ColorEdit3("Color", (float*)&color);
}

VolumeMaterial::VolumeMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	shader = Shader::Get("data/shaders/marching.vs", "data/shaders/marching.fs");

	jittering = true;

	clip_plane_x = -1.0;
	clip_plane_y = -1.0;
	clip_plane_z = -1.0;

	threshold = 0.6;

	light = new Light();
}

VolumeMaterial::~VolumeMaterial()
{
	
}

void VolumeMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_camera_position", camera->eye);
	shader->setUniform("u_model", model);
	shader->setUniform("u_time", Application::instance->time);
	
	shader->setUniform("u_light_position", light->position);

	shader->setUniform("u_color", color);
	shader->setUniform("apply_jittering", jittering);
	shader->setUniform("u_ray_step", step);
	shader->setUniform("u_threshold", threshold);

	shader->setUniform("u_clip_plane_x", clip_plane_x);
	shader->setUniform("u_clip_plane_y", clip_plane_y);
	shader->setUniform("u_clip_plane_z", clip_plane_z);

	shader->setUniform("u_noise_texture", Texture::Get("data/textures/randnoise.png"));
	shader->setUniform("u_transfer_function", Texture::Get("data/textures/rainbowLUT.png"));

	if (texture)
		shader->setUniform("u_texture", texture);
}

void VolumeMaterial::render(Mesh* mesh, Matrix44 model, Camera* camera)
{
	glEnable(GL_DEPTH_TEST);

	glEnable(GL_BLEND);
	// FIXME: No estic molt segur d'això però soluciona part del problema
	// que fa que es vegi part posterior del shader o "discs" al vidre
	glEnable(GL_CULL_FACE);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

	if (mesh && shader)
	{
		//enable shader
		shader->enable();

		//upload uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		//disable shader
		shader->disable();
	}

	glDisable(GL_BLEND);
	glDisable(GL_CULL_FACE);
}

void VolumeMaterial::renderInMenu()
{
	ImGui::Checkbox("Jittering", &jittering);
	ImGui::DragFloat("iso threshold", (float*)&threshold, 0.01, 0.01, 0.99);
	ImGui::DragFloat("ray step", (float*)&step, 0.001, 0.001, 0.5);

	ImGui::DragFloat("clip plane x", (float*)&clip_plane_x, 0.01, -1.0, 1.0);
	ImGui::DragFloat("clip plane y", (float*)&clip_plane_y, 0.01, -1.0, 1.0);
	ImGui::DragFloat("clip plane z", (float*)&clip_plane_z, 0.01, -1.0, 1.0);
}

PBRMaterial::PBRMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);

	light = new Light();
	roughness_factor = 1.0;
	metalness_factor = 1.0;

	with_direct_lighting = true;
	with_indirect_lighting = true;
	with_normal_map = true;
	with_opacity_map = true;
	with_occlusion_map = true;
	with_gamma = true;
	
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/pbr.fs");
}

PBRMaterial::~PBRMaterial()
{

}

void PBRMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	//upload node uniforms
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_camera_position", camera->eye);

	shader->setUniform("u_model", model);
	shader->setUniform("u_color", color);

	shader->setUniform("u_light_position", light->position);
	shader->setUniform("u_light_color", light->diffuse);
	
	shader->setUniform("u_with_direct_lighting", with_direct_lighting);
	shader->setUniform("u_with_indirect_lighting", with_indirect_lighting);

	shader->setUniform("u_roughness_factor", roughness_factor);
	shader->setUniform("u_metalness_factor", metalness_factor);

	shader->setUniform("u_albedo_map", albedo_map, 0);

	shader->setUniform("u_with_normal_map", with_normal_map);
	shader->setUniform("u_normal_map", normal_map, 1);
	
	shader->setUniform("u_with_gamma", with_gamma);

	shader->setUniform("u_metalness_map", metalness_map, 2);
	shader->setUniform("u_roughness_map", roughness_map, 3);

	if (texture) {
		shader->setUniform("u_texture", texture, 4);

		shader->setUniform("u_texture_prem_0", texture_hdre_levels[0], 5);
		shader->setUniform("u_texture_prem_1", texture_hdre_levels[1], 6);
		shader->setUniform("u_texture_prem_2", texture_hdre_levels[2], 7);
		shader->setUniform("u_texture_prem_3", texture_hdre_levels[3], 8);
		shader->setUniform("u_texture_prem_4", texture_hdre_levels[4], 9);
	}

	shader->setUniform("u_with_occlusion_map", with_occlusion_map);
	shader->setUniform("u_occlusion_map", occlusion_map, 11);

	shader->setUniform("u_with_opacity_map", with_opacity_map);
	shader->setUniform("u_opacity_map", opacity_map, 13);
}

void PBRMaterial::setTextures(char* sky_texture)
{
	roughness_map = new Texture();
	normal_map = new Texture();
	metalness_map = new Texture();
	albedo_map = new Texture();
	brdf_lut = new Texture();

	opacity_map = new Texture();
	occlusion_map = new Texture();

	roughness_map->load("data/models/lantern/roughness.png");
	normal_map->load("data/models/lantern/normal.png");
	metalness_map->load("data/models/lantern/metalness.png");
	albedo_map->load("data/models/lantern/albedo.png");
	brdf_lut->load("data/textures/brdfLUT.png");
	opacity_map->load("data/models/lantern/opacity.png");
	occlusion_map->load("data/models/lantern/ao.png");

	HDRE* hdre = HDRE::Get(sky_texture);

	texture = new Texture();
	unsigned int LEVEL = 0;
	texture->cubemapFromHDRE(hdre, LEVEL);

	for (unsigned int i = 1; i < 6; i++)
	{
		Texture* aux_texture = new Texture();
		aux_texture->cubemapFromHDRE(hdre, i);
		texture_hdre_levels[i-1] = aux_texture;
	}
}

void PBRMaterial::render(Mesh* mesh, Matrix44 model, Camera* camera)
{
	//set flags
	glEnable(GL_DEPTH_TEST);

	// We need to enable blending and alpha functions
	// for opacity to work, otherwise alpha channel is 1.0
	// https://stackoverflow.com/a/30164470

	if (with_opacity_map) {
		glEnable(GL_BLEND);	
		// FIXME: No estic molt segur d'això però soluciona part del problema
		// que fa que es vegi part posterior del shader o "discs" al vidre
		glEnable(GL_CULL_FACE);
		glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
	}

	if (mesh && shader)
	{
		//enable shader
		shader->enable();

		//upload uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		//disable shader
		shader->disable();
	}

	if (with_opacity_map) {
		glDisable(GL_BLEND);
		glDisable(GL_CULL_FACE);
	}
}

void PBRMaterial::renderInMenu()
{
	ImGui::Checkbox("With direct lighting", &with_direct_lighting);
	ImGui::Checkbox("With IBL", &with_indirect_lighting);

	ImGui::DragFloat("roughness", (float*)&roughness_factor, 0.01, 0.0, 1.0);
	ImGui::DragFloat("metalness", (float*)&metalness_factor, 0.01, 0.0, 1.0);
	
	ImGui::Checkbox("Normal map", &with_normal_map);
	ImGui::Checkbox("Opacity map", &with_opacity_map);
	ImGui::Checkbox("Occlusion map", &with_occlusion_map);
	
	ImGui::Checkbox("Gamma correction", &with_gamma);
}

PhongMaterial::PhongMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	light = new Light();
	shader = Shader::Get("data/shaders/phong.vs", "data/shaders/phong.fs");

	ambient.set(0.35, 0.36, 0.35);
	diffuse.set(0.80, 0.80, 0.80);
	specular.set(0.95, 0.96, 0.95);

	shininess = 35.0;
}

PhongMaterial::~PhongMaterial()
{
	
}

void PhongMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_camera_position", camera->eye);
	shader->setUniform("u_model", model);
	shader->setUniform("u_time", Application::instance->time);

	shader->setUniform("u_color", color);

	if (texture)
		shader->setUniform("u_texture", texture);

	if (light) {
		shader->setUniform("light_position", light->position);
		shader->setUniform("diffuse_i", light->diffuse);
		shader->setUniform("specular_i", light->specular);
		shader->setUniform("ambient_i", light->ambient);

		shader->setUniform("diffuse_k", diffuse);
		shader->setUniform("specular_k", specular);
		shader->setUniform("ambient_k", ambient);

		shader->setFloat("alpha", shininess);
	}
}

void PhongMaterial::renderInMenu()
{
	ImGui::Text("PHONG PARAMETERS");
	ImGui::SliderFloat("Shininess", (float*)&shininess, 0.0f, 50.0f);
	ImGui::ColorEdit3("Ambient reflection", (float*)&ambient);
	ImGui::ColorEdit3("Diffuse reflection", (float*)&diffuse);
	ImGui::ColorEdit3("Specular reflection", (float*)&specular);
}

SkyboxMaterial::SkyboxMaterial()
{
	shader = Shader::Get("data/shaders/skybox.vs", "data/shaders/skybox.fs");
	texture = new Texture();
}

SkyboxMaterial::~SkyboxMaterial()
{

}

void SkyboxMaterial::setUniforms(Camera* camera)
{	
	mat4 view = camera->view_matrix;
	view.translate(camera->eye.x, camera->eye.y, camera->eye.z);

	shader->setUniform("u_view", view);
	shader->setUniform("u_projection", camera->projection_matrix);

	if (texture)
		shader->setUniform("u_texture", texture);
}

void SkyboxMaterial::render(Mesh* mesh, Matrix44 model, Camera* camera)
{
	//set flags
	glDisable(GL_DEPTH_TEST);
	glDisable(GL_CULL_FACE);

	model.setIdentity();

	if (mesh && shader)
	{
		//enable shader
		shader->enable();

		//upload uniforms
		setUniforms(camera);

		model.setTranslation(camera->eye.x, camera->eye.y, camera->eye.z);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		//disable shader
		shader->disable();
	}
	glEnable(GL_DEPTH_TEST);
}

PhongMirrorMaterial::PhongMirrorMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	light = new Light();
	shader = Shader::Get("data/shaders/phong_reflective.vs", "data/shaders/phong_reflective.fs");

	ambient.set(0.85, 0.86, 0.85);
	diffuse.set(0.80, 0.80, 0.80);
	specular.set(0.95, 0.96, 0.95);

	shininess = 35.0;
}

PhongMirrorMaterial::~PhongMirrorMaterial()
{

}

void PhongMirrorMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_camera_position", camera->eye);
	shader->setUniform("u_model", model);
	shader->setUniform("u_time", Application::instance->time);

	shader->setUniform("u_color", color);

	if (texture)
		shader->setUniform("u_texture", texture);

	if (light) {
		shader->setUniform("light_position", light->position);
		shader->setUniform("diffuse_i", light->diffuse);
		shader->setUniform("specular_i", light->specular);
		shader->setUniform("ambient_i", light->ambient);

		shader->setUniform("diffuse_k", diffuse);
		shader->setUniform("specular_k", specular);
		shader->setUniform("ambient_k", ambient);

		shader->setFloat("alpha", shininess);
	}

}

MirrorMaterial::MirrorMaterial()
{
	shader = Shader::Get("data/shaders/reflective.vs", "data/shaders/reflective.fs");
}

MirrorMaterial::~MirrorMaterial()
{

}

void MirrorMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_camera_position", camera->eye);
	shader->setUniform("u_model", model);

	shader->setUniform("u_color", color);

	if (texture)
		shader->setUniform("u_texture", texture);
}

WireframeMaterial::WireframeMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/flat.fs");
}

WireframeMaterial::~WireframeMaterial()
{

}

void WireframeMaterial::render(Mesh* mesh, Matrix44 model, Camera * camera)
{
	if (shader && mesh)
	{
		glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);

		//enable shader
		shader->enable();

		//upload material specific uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	}
}
