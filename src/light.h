#pragma once

#include "framework.h"

//This class contains all the info about the properties of the light
class Light
{
public:
	Vector3 position; //where is the light
	Vector3 diffuse_color; //the amount (and color) of diffuse
	Vector3 specular_color; //the amount (and color) of specular
	Vector3 ambient_color; //the amount (and color) of ambient

	Light();
};