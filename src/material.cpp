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

PBRMaterial::PBRMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);

	light = new Light();
	roughness_factor = 0.0;
	metalness_factor = 0.0;
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

	shader->setUniform("u_roughness_factor", roughness_factor);
	shader->setUniform("u_metalness_factor", metalness_factor);

	shader->setUniform("u_albedo_map", albedo_map, 0);
	shader->setUniform("u_normal_map", normal_map, 1);
	shader->setUniform("u_metalness_map", metalness_map, 2);
	shader->setUniform("u_roughness_map", roughness_map, 3);

	if (texture_hdre) {
		shader->setUniform("u_texture", texture_hdre, 4);

		shader->setUniform("u_texture_prem_0", texture_hdre_levels[0], 5);
		shader->setUniform("u_texture_prem_1", texture_hdre_levels[1], 6);
		shader->setUniform("u_texture_prem_2", texture_hdre_levels[2], 7);
		shader->setUniform("u_texture_prem_3", texture_hdre_levels[3], 8);
		shader->setUniform("u_texture_prem_4", texture_hdre_levels[4], 9);
	}
}

void PBRMaterial::setTextures(int model)
{
	roughness_map = new Texture();
	normal_map = new Texture();
	metalness_map = new Texture();
	albedo_map = new Texture();
	brdf_lut = new Texture();

	switch (model) {
		case 1:
			roughness_map->load("data/models/helmet/roughness.png");
			normal_map->load("data/models/helmet/normal.png");
			metalness_map->load("data/models/helmet/metalness.png");
			albedo_map->load("data/models/helmet/albedo.png");
			brdf_lut->load("data/textures/brdfLUT.png");
			break;
		case 2:
			roughness_map->load("data/textures/roughness.png");
			normal_map->load("data/textures/normal.png");
			metalness_map->load("data/textures/metalness.png");
			albedo_map->load("data/textures/color.png");
			brdf_lut->load("data/textures/brdfLUT.png");
			break;
		case 3:
			roughness_map->load("data/models/lantern/roughness.png");
			normal_map->load("data/models/lantern/normal.png");
			metalness_map->load("data/models/lantern/metalness.png");
			albedo_map->load("data/models/lantern/albedo.png");
			brdf_lut->load("data/textures/brdfLUT.png");
			break;
		default:
			break;
	}
	

	HDRE* hdre = HDRE::Get("data/environments/panorama.hdre");

	texture_hdre = new Texture();
	unsigned int LEVEL = 0;
	texture_hdre->cubemapFromHDRE(hdre, LEVEL);

	for (unsigned int i = 0; i < 5; i++)
	{
		Texture* aux_texture = new Texture();
		aux_texture->cubemapFromHDRE(hdre, i);
		texture_hdre_levels[i] = aux_texture;
	}
}

void PBRMaterial::renderInMenu()
{

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
}

SkyboxMaterial::~SkyboxMaterial()
{
	texture = new Texture();
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
