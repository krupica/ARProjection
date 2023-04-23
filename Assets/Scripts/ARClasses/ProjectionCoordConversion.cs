using UnityEngine;
using Base;

namespace Assets.Scripts.ARClasses
{
    public static class ProjectionCoordConversion
    {
        public static Vector3 ROSToUnityY0(Vector3 position)
        {
            return new Vector3(-position.y, 0, position.x);
        }

        public static Quaternion ROSToUnityCanvas(Quaternion rotation)
        {
            var test = new Quaternion(rotation.y, -rotation.x, -rotation.z, rotation.w);
            return test;
        }

        // inspirováno https://answers.unity.com/questions/1014337/calculation-behind-cameraworldtoscreenpoint.html
        public static Vector3 ManualWorldToScreenPoint(Vector3 wp)
        {
            Matrix4x4 projInt = ProjectionManager.Instance.calibrationData.ProjInt;
            Camera cam = ProjectionManager.Instance.projector.GetComponent<Camera>();
            // calculate view-projection matrix
            Matrix4x4 mat;
            mat = projInt * cam.worldToCameraMatrix;
            
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
                temp.x = (temp.x / temp.w + 1f) * .5f * cam.pixelWidth - cam.pixelWidth/2;
                temp.y = (temp.y / temp.w + 1f) * .5f * cam.pixelHeight - cam.pixelHeight/2;
                return new Vector3(temp.x, temp.y, wp.z);
            }
        }
    }
}