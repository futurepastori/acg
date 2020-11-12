The goal of this project is to obtain a photo-realistinc rendering by using two main components: a realistic model of how light interacts with surfaces (PBR), and a model of illumination, which essentially captures the light in a real scene, and uses it to illuminate the scene (IBL).
For the implementation, we have created a new material called PBR material which inherits form StandarMaterial, and the shader pbr.fs, that we assign to this material.

# PBR MATERIAL material.cpp / material.h

**TODO:** setUniforms & setTextures

**setUniforms**
Forcing slots manually

# SHADER PBR.fs
The final color of the PBR material will be the addition of the punctual light and the environment light (using IBL). Therefore, the shader follows the next steps:

**1.** Create a PBR material and PBR vectors

**2.** Compute light equation vectors (N, L, V, R, H) as well as the dot products that we are going to need for the computations.
N vec is computed using perturbNormal function provided with the **normal_map** in order to have a non-flat / irregular surface.

**3.** Initialize material properties:
  * roughness - how irregular is a surface. Roughness varies in all the surface, we extract roughness from a roughness texture (_u_roughness_map_) and multiply by the roughness factor that is initialized as 1 by default.
  * metalness - we extract the metalness from a _metalness_map_ and multiply by a metalness factor that is also initialized as 1 by default.
  
  For the previous ones, we take just one channel of the texture depending on the image we have and in which channel the information is located. **TODO: en el cas del helmet no es el r channel, que dic?**
  
  * albedo - is the base color. We extract the albedo values from a 2D texture taking the rgb channels. Also, when doing computations with albedo is essential to do degamma to the color (we convert to linear before doing any computation).

  * Opacity and Occlusion maps - if an opacity map exists, we also create a texture from it and extract one channel. Otherwise, the opacity will be 1.0. The same happens for the occlusion map. **TODO VICTOR: repassa a ver si ho dic bé**

  * c_diff (diffuse colour/ diffuse albedo) - it is a vector of rgb -- For the diffuse color, we do a linear interpolation of a vec3 of zeros and the albedo color, over the metallicfactor. The absorption of color is represented by (1-metalness) * base color (albedo color). It will
  * F0 (specular colour/specular albedo) - it is a linear interpolation of the albedo color and vec3 of 0.04, over the metallic factor. Unlike c_diff, the more specular the material, the more it reflects. 

Next, the shader computes separately the direct and the indirect (environmental) lighting.

4. Compute the **direct lighting** (`f = f_diffuse + f_specular`) with the formulas
  * f_diffuse : with the diffuse colour / pi (_Lambertian diffusion equation_) that give us a constant diffusion in every diretion of the half sphere.
  * f_specular: this computation follows a more complex equation.
  
  **TODO: afegir formula??**
  
  * Fresnel function _F(l,h)_: represents the fraction of light reflected from a flat surface. Depends on the incnident angle and reflective index. Computed with the specular color F0, using the following equation:
  
  **TODO: afegir formula**

  * Geometry function _G(l,v,h)_: represents the proportion of microfacets with m = h that are neither shadowed nor masked. Depends on the roughness. Computed using the following equation:

   **TODO: afegir formula**
where the _k_ is:
  **TODO: afegir formula de la k**

  * Distribution function _D(h)_: is the distribution of the normals of the micro-facets. Computed with the following equation:
**TODO: afegir formula de D i de alpha**

Where alpha is:

Finally, computes direct lighting as `diffuse + specular`. We also multiply the result by the dot product between n and l

5. Compute **indirect lighting**

* Compute LUT_map -- **TODO**: VICTOR EXPLICAR BÉ

* Compute diffuse IBL
* Compute specular IBL (from LUT map)
* Compute IBL color (diffuse IBL + specular IBL)

6. get Pixel color: get final color by adding IBL to the direct lighting (final_color = direct lighting + indirect lighting)

7. Apply tone-mapping for the HDR envoronments to transform the final light to how humans perceive it
8. Apply gamma correction (i.e. raise color intensities to 1/gamma) to display it in the monitor. As the monitors have non-linear color response with respect to raw values passed from the graphic card.



