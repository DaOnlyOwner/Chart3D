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

        float phi,theta, r = 30;
        private float desiredTheta = (float)Math.PI/4;
        private float desiredPhi = (float)Math.PI / 4;

        public Camera()
        {
            proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1, 1, 100000);
        }

        public void UpdateAspect(float aspect)
        {
            proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), aspect, 1, 100000);
        }

        public void Forward(float amount)
        {
            r += amount;
        }
        public void Move(float phi, float theta)
        {
            updatePhi(phi);
            updateTheta(theta);
        }

        private void updatePhi(float phi)
        {
            desiredPhi += phi;
        }

        private void updateTheta(float theta)
        {
            desiredTheta = (float)MathHelper.Clamp(this.theta + theta, 0, Math.PI);
        }

        private void interpolateTheta()
        {
            this.theta = 0.75f * this.theta + 0.25f * desiredTheta;
        }


        private void interpolatePhi()
        {
            this.phi = 0.75f * this.phi + 0.25f * desiredPhi;
        }
       

        public Matrix4 GetV()
        {
            float sintheta = (float) Math.Sin(theta);
            float x = (float) (r * sintheta * Math.Cos(phi));
            float y = (float) (r * sintheta * Math.Sin(phi));
            float z = (float) (r * Math.Cos(theta));

            view = Matrix4.LookAt(x, y, z, 0, 0, 0, 0, 0, 1);         
            return view;


            // The model doesn't move;

        }

        public Matrix4 GetP()
        {
            return proj;
        }

        public void InterpolatePos()
        {
            interpolatePhi();
            interpolateTheta();
        }

        public static Vector3 SphericalToCart(float r, float phi, float theta)
        {
            float sintheta = (float)Math.Sin(theta);
            float x = (float)(r * sintheta * Math.Cos(phi));
            float y = (float)(r * sintheta * Math.Sin(phi));
            float z = (float)(r * Math.Cos(theta));
            return new Vector3(x, y, z);
        }
    }
}