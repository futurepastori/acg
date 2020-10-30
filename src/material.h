#ifndef MATERIAL_H
#define MATERIAL_H

#include "framework.h"
#include "shader.h"
#include "light.h"
#include "camera.h"
#include "mesh.h"

class Material {
public:

	Shader* shader = NULL;
	Texture* texture = NULL;
	vec4 color;

	virtual void setUniforms(Camera* camera, Matrix44 model) = 0;
	virtual void render(Mesh* mesh, Matrix44 model, Camera * camera) = 0;
	virtual void renderInMenu() = 0;
};

class StandardMaterial : public Material {
public:

	StandardMaterial();
	~StandardMaterial();

	void setUniforms(Camera* camera, Matrix44 model);
	void render(Mesh* mesh, Matrix44 model, Camera * camera);
	void renderInMenu();
};

class PBRMaterial : public StandardMaterial {
public:

	Light* light = NULL;
	vec3 diffuseColor;

	PBRMaterial();
	~PBRMaterial();

	void setUniforms(Camera* camera, Matrix44 model);
	void renderInMenu();
};

class PhongMaterial : public StandardMaterial {
public:

	Light* light = NULL;

	vec3 ambient;
	vec3 diffuse;
	vec3 specular;

	float shininess;
	
	PhongMaterial();
	~PhongMaterial();

	void setUniforms(Camera* camera, Matrix44 model);
	void renderInMenu();
};

class SkyboxMaterial : public StandardMaterial {
public:
	
	SkyboxMaterial();
	~SkyboxMaterial();

	void render(Mesh* mesh, Matrix44 model, Camera* camera);
	void setUniforms(Camera* camera);
};

class PhongMirrorMaterial : public StandardMaterial {
public:
	Light* light = NULL;

	vec3 ambient;
	vec3 diffuse;
	vec3 specular;

	float shininess;

	PhongMirrorMaterial();
	~PhongMirrorMaterial();

	void setUniforms(Camera* camera, Matrix44 model);
};

class MirrorMaterial : public StandardMaterial {
public:
	
	MirrorMaterial();
	~MirrorMaterial();

	void setUniforms(Camera* camera, Matrix44 model);
};

class WireframeMaterial : public StandardMaterial {
public:

	WireframeMaterial();
	~WireframeMaterial();

	void render(Mesh* mesh, Matrix44 model, Camera* camera);
};

#endif