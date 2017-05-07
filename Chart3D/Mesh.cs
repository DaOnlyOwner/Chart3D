using System.Collections.Generic;
using OpenTK.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;

namespace Chart3D
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex
    {
        public float x, y, z;
        public float nx, ny, nz;
        public const int Size = 6* sizeof(float); // In bytes
        
        public Vector3 Position { get { return new Vector3(x, y, z); } set { x = value.X; y = value.Y; z = value.Z; } }
        public Vector3 Normal { get { return new Vector3(nx, ny, nz); } set { nx = value.X; ny = value.Y; nz = value.Z; } }


        public Vertex(float x, float y, float z, float nx, float ny, float nz)
        {
            this.x = x; this.y = y; this.z = z;
            this.nx = nx; this.ny = ny; this.nz = nz;
        }

        
        public Vertex(float x, float y, float z)
        {
            this.x = x; this.y = y; this.z = z;
            nx = 0; ny = 0; nz = 0;
        }
        


         
    }
    public sealed class Mesh : IDrawable
    {
        int handleVBO = -1;
        int handleVAO = -1;
        public Mesh(List<Vertex> vertices)
        {
            this.vertices = vertices.ToArray();
            Load();
        }

        public Mesh(Vertex[] vertices)
        {
            this.vertices = vertices;
            Load();
        }

        private Vertex[] vertices;

        public Vector3 Scale { get { return new Vector3(sx, sy, sz); } set { sx = value.X; sy = value.Y; sz = value.Z; } }
        public Matrix4 M { get { return Matrix4.CreateScale(sx, sy, sz); } }
        float sx, sy, sz;

        public void Load()
        {
            if(handleVBO > -1 || handleVAO > -1)
            {
                throw new System.NotSupportedException("Reassigning of handle is not supported right now. TODO");
            }

            GL.GenVertexArrays(1, out handleVAO);
            GL.GenBuffers(1, out handleVBO);

            if (handleVAO < 0 || handleVBO < 0) throw new System.InsufficientMemoryException("When aquiring a vbo / vao -1 was returned. Probably not sufficient memory");

            GL.BindVertexArray(handleVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, handleVBO);
         
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Vertex.Size, vertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vertex.Size, 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vertex.Size, 3*sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        }

        public void Draw()
        {
            GL.BindVertexArray(handleVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Length);
            GL.BindVertexArray(0);
        }



        ~Mesh()
        {
            //GL.DeleteBuffer(handleVBO);
            //GL.DeleteVertexArray(handleVAO);
        }
    }
}