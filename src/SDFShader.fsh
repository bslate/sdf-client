precision mediump float;

uniform sampler2D u_texture;
uniform vec4 u_color;
uniform float u_buffer;
uniform float u_gamma;
uniform float u_debug;

varying vec2 v_texcoord;

void main() {
    float dist = texture2D(u_texture, v_texcoord).r;
    if (u_debug > 0.0) {
        gl_FragColor = vec4(dist, dist, dist, 1);
    } else {
        float alpha = smoothstep(u_buffer - u_gamma, u_buffer + u_gamma, dist);
        gl_FragColor = vec4(u_color.rgb, alpha * u_color.a);
    }
}
