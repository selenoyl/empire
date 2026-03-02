#version 410 core
in vec3 vNormal;
uniform vec3 uLightDir;
uniform vec3 uColor;
uniform float uAmbient;
out vec4 FragColor;
void main(){
    float ndl = max(dot(normalize(vNormal), normalize(-uLightDir)), 0.0);
    vec3 c = uColor * (uAmbient + ndl * (1.0-uAmbient));
    FragColor = vec4(c,1.0);
}
