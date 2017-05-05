using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Chart3D
{
    public delegate float Calc(float x, float y);

    //TODO: Implement a Constructor that takes an IEnumerable to abstract from delegates
    //TODO: Implement wrapper around Matrix4 so that it doesn't get copied all the time
    public class Chart3D : OpenTK.GameWindow
    {
        private Mesh procMesh;
        private Camera cam = new Camera();
        ShaderProgram program;
        bool canMove = false;
        Calc gen;
        float stepX, stepY;
        float rangeX, rangeY;

        int lastMouseX, lastMouseY;

        public Chart3D(Calc gen, float stepX = 1f, float stepY = 1f, float rangeX=10f, float rangeY = 10f, float lengthXYAxis = 10f, float lengthZAxis = 10f) : base(800, 600, GraphicsMode.Default, "Function Plot", GameWindowFlags.Default, DisplayDevice.Default, 3, 3, GraphicsContextFlags.Default)
        {
            MouseDown += Chart3D_MouseDown;
            MouseUp += Chart3D_MouseUp;
            UpdateFrame += Chart3D_UpdateFrame;
            Load += initOpenGL;

            this.gen = gen;
            this.stepX = stepX;
            this.stepY = stepY;
            this.rangeX = rangeX;
            this.rangeY = rangeY;

        }

        private void initOpenGL(object sender, EventArgs e)
        {
            program = new ShaderProgram(VShader, FShader);
            List<Vertex> vertices = CalculateMesh(gen, stepX, stepY, rangeX, rangeY);
            procMesh = new Mesh(vertices);
            GL.Enable(EnableCap.DepthTest);
            OnResize(e);
        }

        private void Chart3D_UpdateFrame(object sender, FrameEventArgs e)
        {
            if (canMove)
            {
                int deltaX = lastMouseX - Mouse.X;
                int deltaY = lastMouseY - Mouse.Y;
                cam.Move(deltaX * (float)e.Time * 0.7f, deltaY * (float)e.Time * 0.7f);
                lastMouseX = Mouse.X; lastMouseY = Mouse.Y;
            }


            cam.InterpolatePos();
            cam.Forward(lastWheel - Mouse.Wheel);
            lastWheel = Mouse.Wheel;

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
            lastMouseX = Mouse.X; lastMouseY = Mouse.Y;
            canMove = true;
        }




        List<Vertex> CalculateMesh(Calc gen, float stepX, float stepY, float width, float height)
        {
            var verts = new List<Vertex>();
            for (float y = -height; y < height-1; y += stepY)
            {
                for(float x = -width; x<width; x+=stepX)
                {
                    // Calculate the positions
                    float offsetX = x + stepX;
                    float offsetY = y + stepY;
                    Vertex v1 = new Vertex(x, y, gen(x, y));
                    Vertex v2 = new Vertex(x, offsetY, gen(x, offsetY));
                    Vertex v3 = new Vertex(offsetX, offsetY, gen(offsetX, offsetY));
                    Vertex v4 = new Vertex(offsetX, y, gen(offsetX,y));

                    // Calculate the normals
                    /*Vector3 d1 = v2.Position - v1.Position;
                    Vector3 d2 = v4.Position - v1.Position;
                    v1.Normal = Vector3.Cross(d2, d1).Normalized();
                    v2.Normal = v1.Normal;
                    v3.Normal = v1.Normal;
                    v4.Normal = v1.Normal;*/

                    verts.Add(v1); verts.Add(v2); verts.Add(v4); verts.Add(v4);  verts.Add(v3); verts.Add(v2);

                    
                }
            }

            var max = verts.Max((v) => v.z);
            for(int i = 0; i<verts.Count; i++)
            {
                verts[i] = new Vertex(verts[i].x / width * 10, verts[i].y / height * 10, verts[i].z / max * 10);
            }

            return verts;
            //eturn new List<Vertex>() { new Vertex(0, 0, 0), new Vertex(0, 1, 0), new Vertex(0, 0, 1) };

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

out float height;
out vec4 V;
out vec4 N;

void main()
{
    vec4 posAsVec4 = vec4(position,1.0);
    gl_Position = (P*MV) * posAsVec4;
    height = posAsVec4.z;
    V = - MV * posAsVec4;
    N = MV * vec4(normal,1.0); // No scaling involved
}
";

        private static string FShader = @"
#version 330
in float height;
in vec4 V;
in vec4 N;
out vec4 fragColor;
void main()
{
    fragColor = vec4(tan(height),sin(height),cos(height),1);
}
";
        private int lastWheel;
    }
}
