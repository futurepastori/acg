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

PhongMaterial::PhongMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	light = new Light();
	shader = Shader::Get("data/shaders/phong_reflective.vs", "data/shaders/phong_reflective.fs");

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

SkyboxMaterial::SkyboxMaterial()
{
	shader = Shader::Get("data/shaders/skybox.vs", "data/shaders/skybox.fs");
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
