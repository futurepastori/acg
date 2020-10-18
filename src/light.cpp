#include "light.h"

Light::Light()
{
	position.set(30, 30, 0);

	diffuse.set(0.6f, 0.6f, 0.6f);
	specular.set(0.3f, 0.3f, 0.3f);
	ambient.set(0.2f, 0.2f, 0.2f);
}