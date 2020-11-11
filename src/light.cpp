#include "light.h"

Light::Light()
{
	position.set(50.0, 50.0, 00.0);

	diffuse.set(1.0f, 1.0f, 1.0f);
	specular.set(1.0f, 1.0f, 1.0f);
	ambient.set(1.0f, 1.0f, 1.0f);
}