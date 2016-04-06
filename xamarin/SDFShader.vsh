attribute vec2 a_pos;
attribute vec2 a_texcoord;

uniform mat4 u_matrix;
uniform vec2 u_texsize;

varying vec2 v_texcoord;

void main() {
    gl_Position = u_matrix * vec4(a_pos.xy, 0, 1);
    v_texcoord = a_texcoord / u_texsize;
}
