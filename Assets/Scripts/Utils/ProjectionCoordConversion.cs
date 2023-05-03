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

        public static Quaternion ROSToCanvas(Quaternion rotation)
        {
            var test = new Quaternion(rotation.y, -rotation.x, rotation.z, rotation.w);
            return test;
        }
        public static Vector3 ROSToCanvasScale(Vector3 scale)
        {
            return new Vector3(scale.y, scale.x, scale.z);
        }

        // inspirováno https://answers.unity.com/questions/1014337/calculation-behind-cameraworldtoscreenpoint.html
        public static Vector2 ManualWorldToScreenPoint(Vector3 wp)
        {
            //vnitrni parametry projektoru
            CalibrationData calibData = ProjectionManager.Instance.calibrationData;
            Matrix4x4 projIntrinsic = calibData.ProjInt;
            GameObject projector = ProjectionManager.Instance.projector;
            Camera cam = ProjectionManager.Instance.projector.GetComponent<Camera>();
            //prevod do souradnicoveho systemu projektoru
            Matrix4x4 worldToCam = projector.transform.worldToLocalMatrix;

            Vector4 localPoint = worldToCam * new Vector4(wp.x, wp.y, wp.z, 1f);
            localPoint.y = -localPoint.y;

            // vynasobeni bodu matici
            Vector4 temp = projIntrinsic * localPoint;

            if (temp.w == 0f)
            {
                // bod je na ohniskovem bode, neni definovan
                return Vector3.zero;
            }
            else
            {
                // prevede souradnice z clip space na souradnice platna
                temp.x = (temp.x / temp.w + 1f) * .5f * calibData.Width - calibData.Width / 2;
                temp.y = (temp.y / temp.w + 1f) * .5f * -calibData.Height + calibData.Height;
                return new Vector2(temp.x, temp.y);
            }
        }
        public static Vector2 RemoveDistortion(Vector2 inputPoint)
        {
            CalibrationData calibData = ProjectionManager.Instance.calibrationData;

            float r2 = inputPoint.x * inputPoint.x + inputPoint.y * inputPoint.y;
            float radialDistortion = 1 + calibData.K1 * r2 + calibData.K2 * r2 * r2 + calibData.K3 * r2 * r2 * r2;

            // Calculate tangential distortion correction
            float deltaX = 2.0f * calibData.P1 * inputPoint.x * inputPoint.y + calibData.P2 * (r2 + 2.0f * inputPoint.x * inputPoint.x);
            float deltaY = calibData.P1 * (r2 + 2.0f * inputPoint.y * inputPoint.y) + 2.0f * calibData.P2 * inputPoint.x * inputPoint.y;

            // Apply distortion correction to input point
            Vector2 outputPoint = new Vector2();
            outputPoint.x = inputPoint.x * radialDistortion + deltaX;
            outputPoint.y = inputPoint.y * radialDistortion + deltaY;

            return outputPoint;
        }
    }
}