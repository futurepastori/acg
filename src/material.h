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

class VolumeMaterial : public StandardMaterial {
public:
	
	bool jittering;
	float step;
	float threshold;

	float clip_plane_x;
	float clip_plane_y;
	float clip_plane_z;

	VolumeMaterial();
	~VolumeMaterial();

	void setUniforms(Camera* camera, Matrix44 model);
	void render(Mesh* mesh, Matrix44 model, Camera* camera);
	void renderInMenu();
};

class PBRMaterial : public StandardMaterial {
public:

	Light* light = NULL;

	PBRMaterial();
	~PBRMaterial();

	// maps
	float roughness_factor;
	Texture* roughness_map;

	float metalness_factor;
	bool with_direct_lighting;
	bool with_indirect_lighting;
	Texture* metalness_map;

	bool with_normal_map;
	Texture* normal_map;
	Texture* albedo_map;
	Texture* brdf_lut;
	
	// Optional maps
	bool with_occlusion_map;
	Texture* occlusion_map;
	bool with_opacity_map;
	Texture* opacity_map;
	
	bool with_gamma;

	// We create a collection of HDR cubemaps sorted by blur
	// level (L2 slides, pp. 35)
	Texture* texture_hdre_levels[5];

	void setUniforms(Camera* camera, Matrix44 model);
	void setTextures(char* sky_texture);
	void render(Mesh* mesh, Matrix44 model, Camera * camera);
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

	bool hdre;

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