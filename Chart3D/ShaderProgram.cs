﻿using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chart3D
{
    public class ShaderProgram
    {

        int handleVS, handleFS, handleProg;

        public ShaderProgram(String vs, String fs)
        {
            handleVS = GL.CreateShader(ShaderType.VertexShader);
            handleFS = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(handleVS, vs);
            GL.ShaderSource(handleFS, fs);
            GL.CompileShader(handleVS);
            testShader(handleVS,"Vertex");
            GL.CompileShader(handleFS);
            testShader(handleFS, "Fragment");
            handleProg = GL.CreateProgram();
            GL.AttachShader(handleProg, handleVS);
            GL.AttachShader(handleProg, handleFS);
            GL.LinkProgram(handleProg);
            testLinker();

            GL.DeleteShader(handleVS);
            GL.DeleteShader(handleFS);
            
        }

        public void Use()
        {
            GL.UseProgram(handleProg);
        }

        private void testLinker()
        {
            int success;
            GL.GetProgram(handleProg, GetProgramParameterName.LinkStatus, out success);
            if(success == 0)
            {
                throw new InvalidProgramException("Linking of program failed with message: " + GL.GetProgramInfoLog(handleProg));
            }
        }

        private void testShader(int handle, String which)
        {
            int success;
            GL.GetShader(handle, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                throw new InvalidProgramException(String.Format("The sourcecode of the {0}shader was invalid: {1}", which, GL.GetShaderInfoLog(handleVS)));
            }
        }

        public void SetMatrix(string name, ref Matrix4 matrix)
        {
            Use(); // TODO: test this out --> Do I have to use it here? donno really.
            int loc = GL.GetUniformLocation(handleProg, name);
            GL.UniformMatrix4(loc, false, ref matrix);
        }

        public void SetFloat(string name, float num)
        {
            Use();
            int loc = GL.GetUniformLocation(handleProg, name);
            GL.Uniform1(loc, num);
        }

        internal void SetVector3(string name, ref Vector3 p)
        {
            Use();
            int loc = GL.GetUniformLocation(handleProg, name);
            GL.Uniform3(loc, p);
        }
    }
}