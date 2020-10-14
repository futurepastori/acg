#include "skybox.h"
#include "application.h"
#include "texture.h"
#include "material.h"

Skybox::Skybox() 
{
	mesh = new Mesh();
	mesh->createCube();

	material = new StandardMaterial();
	material->shader = Shader::Get("data/shaders/basic.vs", "data/shaders/flat.fs");
}

Skybox::~Skybox()
{

}

Skybox::Skybox(Texture* tex) 
{
	name = "Skybox";
	mesh = Mesh::Get("data/meshes/box.ASE");

	material = new StandardMaterial();
	material->shader = Shader::Get("data/shaders/basic.vs", "data/shaders/texture.fs");
	material->texture = tex;

	model.scale(100, 100, 100);
}

void Skybox::render(Camera* camera)
{
	model.translate(camera->eye.x, camera->eye.y, camera->eye.z);
	
	if (mesh && material) {
		glDisable(GL_DEPTH_TEST);
		material->render(mesh, model, camera);
		glEnable(GL_DEPTH_TEST);
	}
}