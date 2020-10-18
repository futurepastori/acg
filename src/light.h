#pragma once

#include "framework.h"

//This class contains all the info about the properties of the light
class Light
{
public:
	vec3 position; //where is the light
	vec3 diffuse; //the amount (and color) of diffuse
	vec3 specular; //the amount (and color) of specular
	vec3 ambient; //the amount (and color) of ambient

	Light();
};