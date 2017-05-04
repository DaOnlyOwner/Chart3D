using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace Chart3D
{
    public delegate float Calc(float x, float y);



    public class Chart3D : OpenTK.GameWindow
    {
        private Mesh procMesh;
        private Camera cam = new Camera();

        int handle;
        public Chart3D(Calc gen, int stepX, int stepY, int width, int height) : base(800, 600, GraphicsMode.Default, "Function Plot", GameWindowFlags.Default, DisplayDevice.Default, 3, 3, GraphicsContextFlags.Default)
        {
            List<Vertex> vertices = CalcNormals(CalculateMesh(gen, stepX, stepY, width, height));
            procMesh = new Mesh(vertices);
        }

        List<Vertex> CalculateMesh(Calc gen, int stepX, int stepY, int width, int height)
        {
            var verts = new List<Vertex>();
            for (float y = 0; y < height; y += stepY)
            {
                for(float x = 0; x<width; x+=stepX)
                {
                    Vertex v = new Vertex(x, y, gen(x, y));
                    verts.Add(v);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.ClearColor(Color4.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            procMesh.Draw();           
        }



        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            // Mouse and keyboard 
        }



        private static string VShader = @"
#version 330
uniform mat4 MV;
uniform mat4 P;

in vec3 position;
in vec3 normal;

out vec4 color;
out vec4 V;
out vec3 N;

void main()
{
    vec4 posAsVec4 = vec4(position,1.0);
    gl_position = P*MV * posAsVec4;
    color = posAsVec4;
    V = - MV * vec3(position,1.0);
    N = MV * normal;
}
";

        private static string FShader = @"
#version 330
in vec4 color;
in vec4 V;
out vec4 fragColor;
void main()
{
    fragColor = color * dot(V,N);
}
";
    }
}
