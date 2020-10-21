#include "light.h"

Light::Light()
{
	position.set(13, 13, 0);

	diffuse.set(0.7f, 0.7f, 0.7f);
	specular.set(1.0f, 1.0f, 1.0f);
	ambient.set(0.4f, 0.4f, 0.4f);
}