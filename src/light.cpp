#include "light.h"

Light::Light()
{
	position.set(20.0, 20.0, 0);

	diffuse.set(1.0f, 1.0f, 1.0f);
	specular.set(1.0f, 1.0f, 1.0f);
	ambient.set(1.0f, 1.0f, 1.0f);
}