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

    //TODO: Calculate the normals correctly, improve performance of CalculateMesh.
    //TODO: Implement wrapper around Matrix4 so that it doesn't get copied all the time
    //TODO: Implement wrapper around light
    //TODO: Put calculation of normal matrix to the cpu
    //TODO: Style guidelines;
    //TODO: Correct and optimize shader
    //TODO: Implement a Constructor that takes an IEnumerable to abstract from delegates
    public class Chart3D : OpenTK.GameWindow
    {
        private Mesh procMesh;
        private Camera cam = new Camera();
        ShaderProgram program;
        bool canMove = false;
        Calc gen;
        float stepX, stepY;
        float rangeX, rangeY;
        float lengthZAxis;

        float lightPosPhi;
        float lightPosTheta;
        float lightPosR=10;

        public Chart3D(Calc gen, float stepX = 1f, float stepY = 1f, float rangeX=10f, float rangeY = 10f, float lengthXYAxis = 10f, float lengthZAxis = 10f, int msaaSamples = 4) : base(800, 600, new GraphicsMode(32,24,0,msaaSamples), "Function Plot", GameWindowFlags.Default, DisplayDevice.Default, 3, 3, GraphicsContextFlags.Default)
        {
            MouseDown += Chart3D_MouseDown;
            MouseUp += Chart3D_MouseUp;
            MouseMove += Chart3D_MouseMove;
            MouseWheel += Chart3D_MouseWheel;
            UpdateFrame += Chart3D_UpdateFrame;
            Load += initOpenGL;
            
            this.gen = gen;
            this.stepX = stepX;
            this.stepY = stepY;
            this.rangeX = rangeX;
            this.rangeY = rangeY;

            this.lengthZAxis = lengthZAxis;

        }

        private void Chart3D_MouseMove(object sender, MouseMoveEventArgs e)
        {

            if(canMove && Keyboard.GetState().IsKeyDown(Key.ShiftLeft))
            {
                lightPosPhi -= e.XDelta * 0.01f;
                lightPosTheta -= e.YDelta * 0.03f;
                return;
            }

            if(canMove)
            {
                cam.Move(-e.XDelta * 0.01f, -e.YDelta * 0.03f);
            }
        }

        private void initOpenGL(object sender, EventArgs e)
        {
            List<Vertex> vertices = CalculateMesh(gen, stepX, stepY, rangeX, rangeY);
            procMesh = new Mesh(vertices);
            program = new ShaderProgram(VShader, FShader);
            program.Use();
            program.SetFloat("max_z_value", lengthZAxis);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Multisample);
            
            OnResize(e);
        }

        private void Chart3D_UpdateFrame(object sender, FrameEventArgs e)
        {
            cam.InterpolatePos();
        }

        private void Chart3D_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.GetState().IsKeyDown(Key.ShiftLeft))
            {
                lightPosR -= e.DeltaPrecise;
                return;
            }

            cam.Forward(-e.DeltaPrecise);
        }

        private void Chart3D_MouseUp(object sender, MouseButtonEventArgs e)
        {
            canMove = false;
        }

        private void Chart3D_MouseDown(object sender, MouseButtonEventArgs e)
        {
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
                    Vector3 d1 = v2.Position - v1.Position;
                    Vector3 d2 = v4.Position - v1.Position;
                    v1.Normal = Vector3.Cross(d2, d1).Normalized();
                    v2.Normal = v1.Normal;
                    v3.Normal = v1.Normal;
                    v4.Normal = v1.Normal;
                    //Console.Write(v1.Normal);

                    verts.Add(v1); verts.Add(v2); verts.Add(v4); verts.Add(v4);  verts.Add(v3); verts.Add(v2);

                    
                }
            }

            var max = verts.Max((v) => Math.Abs(v.z));
       
            for(int i = 0; i<verts.Count; i++)
            {
                //verts[i] = new Vertex(verts[i].x / width * 10, verts[i].y / height * 10, verts[i].z / max * 10, verts[i].nx,verts[i].ny,verts[i].nz);
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

            Matrix4 V = cam.GetV();
            Matrix4 P = cam.GetP();
            Vector3 lightPos = Camera.SphericalToCart(lightPosR, lightPosPhi, lightPosTheta);

            program.Use();
            program.SetMatrix("V", ref V);
            program.SetMatrix("P", ref P);
            program.SetVector3("lightPos", ref lightPos);
            procMesh.Draw();
            SwapBuffers();
            Console.WriteLine("Frame took : " + e.Time * 100 + "ms");
        }

        private static string VShader = @"
#version 330

uniform float max_z_value; // Replace this with a constant and set the variable on compile time
uniform mat4 V;
uniform mat4 P;
uniform vec3 lightPos;

in vec3 position;
in vec3 normal;

out float height;
out vec4 E;
out vec4 N;
out vec4 L;

void main()
{
    vec4 posAsVec4 = vec4(position,1.0);
    gl_Position = (P*V) * posAsVec4;
    height = ((posAsVec4.z / max_z_value) +1) / 2.0 ; // Map (posAsVec4.z / max_z_value) from [-1,1] to [0,1]
    E = normalize(- V * posAsVec4);

    N = normalize(vec4( mat3( transpose( inverse(V) ) ) * normal,1.0f)); // No scaling involved
    L = normalize(-V*vec4(lightPos,1.0));
}
";
        // colormap code taken from: https://github.com/kbinani/glsl-colormap/blob/master/shaders/IDL_CB-YIGnBu.frag
        private static string FShader = @"
#version 330


in float height;
in vec4 E;
in vec4 N;
in vec4 L;

out vec4 fragColor;

const float specularStrength = 0.4f;


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

vec4 diffuse()
{
    return vec4(max(dot(N,L),0.0)); // color of light is white;
}

vec4 ambient()
{
    return 0.4 * colormap(height);
}

vec4 specular()
{
    vec4 I = reflect(-L,N);
    float specularComp = pow(max(dot(E,I),0.0),128);
    return vec4(specularStrength * specularComp);
}

void main()
{
    fragColor = (diffuse() + ambient() + specular() ) * colormap(height); 
}
";
    }
}
