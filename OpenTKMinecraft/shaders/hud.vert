﻿#version 460 core
#include "hud_uniforms.glsl"

out vec3 vs_position;
out vec2 vs_texcoord;

void main(void)
{
    gl_Position = position;

    vs_position = position.xyz;
    vs_texcoord = vec2(position.x / 2 + 0.5, 0.5 - position.y / 2);
}
