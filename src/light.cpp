#include "light.h"

Light::Light()
{
	position.set(20.0f, 20.0f, 10.0f);

	diffuse.set(1.0f, 1.0f, 1.0f);
	specular.set(1.0f, 1.0f, 1.0f);
	ambient.set(1.0f, 1.0f, 1.0f);
}