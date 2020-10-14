#pragma once

#include "framework.h"

#include "scenenode.h"
#include "mesh.h"
#include "material.h"
#include "camera.h"
#include "texture.h"

class Skybox : public SceneNode {
	public:

		Skybox();
		Skybox(Texture* texture);
		~Skybox();

		void render(Camera* camera);
};