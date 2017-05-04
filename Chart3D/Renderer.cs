using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Collections.Generic;

namespace Chart3D
{
    public delegate float Calc(float x, float y);


    //TODO: Implement wrapper around Matrix4 so that it doesn't get copied all the time
    public class Chart3D : OpenTK.GameWindow
    {
        private Mesh procMesh;
        private Camera cam = new Camera();
        ShaderProgram program;
        bool canMove = false;
        Calc gen;
        float stepX, stepY;
        int width, height;

        public Chart3D(Calc gen, float stepX, float stepY, int width, int height) : base(800, 600, GraphicsMode.Default, "Function Plot", GameWindowFlags.Default, DisplayDevice.Default, 3, 3, GraphicsContextFlags.Default)
        {
            MouseDown += Chart3D_MouseDown;
            MouseUp += Chart3D_MouseUp;
            MouseWheel += Chart3D_MouseWheel;
            MouseMove += Chart3D_MouseMove;
            UpdateFrame += Chart3D_UpdateFrame;
            Load += initOpenGL;

            this.gen = gen;
            this.stepX = stepX;
            this.stepY = stepY;
            this.width = width;
            this.height = height;

        }

        private void initOpenGL(object sender, EventArgs e)
        {
            program = new ShaderProgram(VShader, FShader);
            List<Vertex> vertices = CalculateMesh(gen, stepX, stepY, width, height);
            procMesh = new Mesh(vertices);
        }

        //TODO: Implement time independent movement of camera.
        private void Chart3D_UpdateFrame(object sender, FrameEventArgs e)
        {
            
        }

        private void Chart3D_MouseMove(object sender, MouseMoveEventArgs e)
        {
            if (!canMove) return;
            cam.Yaw(e.XDelta);
            cam.Pitch(e.YDelta);
        }

        private void Chart3D_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            cam.Forward(e.DeltaPrecise);
        }

        private void Chart3D_MouseUp(object sender, MouseButtonEventArgs e)
        {
            canMove = false;
        }

        private void Chart3D_MouseDown(object sender, MouseButtonEventArgs e)
        {
            canMove = true;
        }




        List<Vertex> CalculateMesh(Calc gen, float stepX, float stepY, int width, int height)
        {
            Console.Write("Test");
            var verts = new List<Vertex>();
            for (float y = 0; y < height-1; y += stepY)
            {
                for(float x = 0; x<width-1; x+=stepX)
                {
                    Console.Write("Test inner" + x.ToString() + " " + y.ToString());
                    // Calculate the positions
                    float offsetX = x + stepX;
                    float offsetY = y + stepY;
                    Vertex v1 = new Vertex(x, y, gen(x, y));
                    Vertex v2 = new Vertex(x, offsetY, gen(x, offsetY));
                    Vertex v3 = new Vertex(offsetX, offsetY, gen(offsetX, offsetY));
                    Vertex v4 = new Vertex(x + 1, offsetY, gen(x+1,offsetY));

                    // Calculate the normals
                    Vector3 d1 = v2.Position - v1.Position;
                    Vector3 d2 = v4.Position - v1.Position;
                    v1.Normal = Vector3.Cross(d2, d1).Normalized();
                    v2.Normal = v1.Normal;
                    v3.Normal = v1.Normal;
                    v4.Normal = v1.Normal;

                    verts.Add(v1); verts.Add(v2); verts.Add(v3); verts.Add(v4);
                }
            }

            return verts;

        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            cam.UpdateAspect(Width / (float) Height);
            GL.Viewport(0, 0, Width, Height);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            GL.ClearColor(Color4.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            program.Use();
            program.SetMatrix("MV", cam.GetMV());
            program.SetMatrix("P", cam.GetP());

            procMesh.Draw();
            SwapBuffers();
        }

        private static string VShader = @"
#version 330
uniform mat4 MV;
uniform mat4 P;

in vec3 position;
in vec3 normal;

out vec4 color;
out vec4 V;
out vec4 N;

void main()
{
    vec4 posAsVec4 = vec4(position,1.0);
    gl_Position = (P*MV) * posAsVec4;
    color = posAsVec4;
    V = - MV * posAsVec4;
    N = MV * vec4(normal,1.0); // No scaling involved
}
";

        private static string FShader = @"
#version 330
in vec4 color;
in vec4 V;
in vec4 N;
out vec4 fragColor;
void main()
{
    fragColor = color * dot(V,N);
}
";
    }
}
