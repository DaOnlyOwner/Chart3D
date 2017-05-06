using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Imaging;

namespace Chart3D
{
    public delegate float Calc(float x, float y);

    //TODO: Implement a Constructor that takes an IEnumerable to abstract from delegates
    //TODO: Implement wrapper around Matrix4 so that it doesn't get copied all the time
    //TODO: Set max_z_value before compile time of shader;
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

        float lengthZAxis;

        public Chart3D(Calc gen, float stepX = 1f, float stepY = 1f, float rangeX=10f, float rangeY = 10f, float lengthXYAxis = 10f, float lengthZAxis = 10f, int msaaSamples = 4) : base(800, 600, new GraphicsMode(32,0,0,msaaSamples), "Function Plot", GameWindowFlags.Default, DisplayDevice.Default, 3, 3, GraphicsContextFlags.Default)
        {
            // TODO: To implement a color map see the paper: http://www.kennethmoreland.com/color-maps/ 
            //String.Format(VShader, lengthZAxis); // To map the highest coordinate to red;

            MouseDown += Chart3D_MouseDown;
            MouseUp += Chart3D_MouseUp;
            UpdateFrame += Chart3D_UpdateFrame;
            Load += initOpenGL;
            
            this.gen = gen;
            this.stepX = stepX;
            this.stepY = stepY;
            this.rangeX = rangeX;
            this.rangeY = rangeY;

            this.lengthZAxis = lengthZAxis;

        }

        private void initOpenGL(object sender, EventArgs e)
        {
            List<Vertex> vertices = CalculateMesh(gen, stepX, stepY, rangeX, rangeY);
            procMesh = new Mesh(vertices);
            program = new ShaderProgram(VShader, FShader);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Multisample);
            
            OnResize(e);
        }

        private void Chart3D_UpdateFrame(object sender, FrameEventArgs e)
        {
            if (canMove)
            {
                int deltaX = lastMouseX - Mouse.X;
                int deltaY = lastMouseY - Mouse.Y;
                cam.Move(deltaX * /*(float)e.Time **/ 0.01f, deltaY /** (float)e.Time */* 0.03f);
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
            
            GL.ClearColor(Color4.White);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            program.Use();
            program.SetMatrix("MV", cam.GetMV());
            program.SetMatrix("P", cam.GetP());
            program.SetFloat("max_z_value", lengthZAxis);
            procMesh.Draw();
            SwapBuffers();
            Console.WriteLine("Frame took : " + e.Time * 100 + "ms");
        }

        private static string VShader = @"
#version 330

uniform float max_z_value; // Replace this with a constant and set the variable on compile time
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
    height = posAsVec4.z / max_z_value;
    V = - MV * posAsVec4;

    N = MV * vec4(normal,1.0); // No scaling involved
}
";
        // colormap code taken from: https://github.com/kbinani/glsl-colormap/blob/master/shaders/IDL_CB-YIGnBu.frag
        private static string FShader = @"
#version 330

in float height;
in vec4 V;
in vec4 N;
out vec4 fragColor;

const float M = 80f;
const float h = 0.5f;
const float s = 1.08f;

vec4 colormap(float x) {
    float r = 0.0, g = 0.0, b = 0.0;

    if (x < 0.0) {
        r = 127.0 / 255.0;
    } else if (x <= 1.0 / 9.0) {
        r = 1147.5 * (1.0 / 9.0 - x) / 255.0;
    } else if (x <= 5.0 / 9.0) {
        r = 0.0;
    } else if (x <= 7.0 / 9.0) {
        r = 1147.5 * (x - 5.0 / 9.0) / 255.0;
    } else {
        r = 1.0;
    }

    if (x <= 1.0 / 9.0) {
        g = 0.0;
    } else if (x <= 3.0 / 9.0) {
        g = 1147.5 * (x - 1.0 / 9.0) / 255.0;
    } else if (x <= 7.0 / 9.0) {
        g = 1.0;
    } else if (x <= 1.0) {
        g = 1.0 - 1147.5 * (x - 7.0 / 9.0) / 255.0;
    } else {
        g = 0.0;
    }

    if (x <= 3.0 / 9.0) {
        b = 1.0;
    } else if (x <= 5.0 / 9.0) {
        b = 1.0 - 1147.5 * (x - 3.0 / 9.0) / 255.0;
    } else {
        b = 0.0;
    }

    return vec4(r, g, b, 1.0);
}


void main()
{
    fragColor = colormap(max(height,0));
}
";
        private int lastWheel;
    }
}
