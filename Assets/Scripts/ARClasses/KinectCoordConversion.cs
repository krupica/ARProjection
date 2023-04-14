using UnityEngine;
using Base;

namespace Assets.Scripts.ARClasses
{
    public static class KinectCoordConversion
    {
        public static void SetProjectorTransform(GameObject projector)
        {
            CalibrationData calib = ProjectionManager.Instance.calibrationData;
            Camera cam = ProjectionManager.Instance.kinect.GetComponent<Camera>();

            // Compute the projector-to-world matrix P
            Matrix4x4 P;
            P = cam.cameraToWorldMatrix * calib.Rotation * Matrix4x4.Translate(calib.Translation);
            //P = cam.cameraToWorldMatrix;

            // Compute the world-to-projector matrix Pinv
            Matrix4x4 Pinv = P.inverse;
            Pinv = P;

            // Compute the projector transform
            projector.transform.position = Pinv.MultiplyPoint(Vector3.zero);
            projector.transform.rotation = Quaternion.LookRotation(-Pinv.GetColumn(2), Pinv.GetColumn(1));
        }

        public static Vector3 ManualWorldToScreenPoint(Vector3 wp)
        {
            Camera cam = ProjectionManager.Instance.kinect.GetComponent<Camera>();
            // calculate view-projection matrix
            Matrix4x4 mat;
            mat = cam.projectionMatrix * cam.worldToCameraMatrix;
            
            // multiply world point by VP matrix
            Vector4 temp = mat * new Vector4(wp.x, wp.y, wp.z, 1f);

            if (temp.w == 0f)
            {
                // point is exactly on camera focus point, screen point is undefined
                return Vector3.zero;
            }
            else
            {
                // convert x and y from clip space to window coordinates
                temp.x = (temp.x / temp.w + 1f) * .5f * cam.pixelWidth;
                temp.y = (temp.y / temp.w + 1f) * .5f * cam.pixelHeight;
                return new Vector3(temp.x, temp.y, wp.z);
            }
        }
    }
}