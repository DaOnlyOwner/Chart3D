using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chart3D
{
    public class Camera
    {
        Matrix4 view;
        Matrix4 proj;

        float phi, theta, r; 

        public Camera()
        {
            proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1, 0, 10000);
        }

        public void UpdateAspect(float aspect)
        {
            proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), aspect, 0, 10000);
        }

        public void Forward(float amount)
        {
            r += amount;
        }
        public void Yaw(float amount)
        {
            phi += amount;
        }

        public void Pitch(float amount)
        {
            theta += amount;
        }
        
        public Matrix4 GetMV()
        {
            float sintheta = (float) Math.Sin(theta);
            float x = (float) (r * sintheta * Math.Cos(phi));
            float y = (float) (r * sintheta * Math.Sin(phi));
            float z = (float) (r * Math.Cos(theta));

            view = Matrix4.CreateTranslation(-x, -y, -z);
            return view;

            // The model doesn't move;

        }

        public Matrix4 GetP()
        {
            return proj;
        }

    }
}