﻿using UnityEngine;

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
            var test = new Quaternion(0, 0, rotation.z, rotation.w);
            return test;
        }
        public static Vector3 ROSToCanvasScale(Vector3 scale)
        {
            float scaleModifier = ProjectionManager.Instance.scaleModifier;
            return new Vector3(scale.x * scaleModifier, scale.y * scaleModifier, scale.z * scaleModifier);
        }

        // inspirováno https://answers.unity.com/questions/1014337/calculation-behind-cameraworldtoscreenpoint.html
        public static Vector2 ManualWorldToScreenPoint(Vector3 wp)
        {
            CalibrationData calibData = ProjectionManager.Instance.calibrationData;
            Matrix4x4 projIntrinsic = calibData.ProjInt;
            GameObject projector = ProjectionManager.Instance.projector;
            //get position in projectors local space
            Matrix4x4 worldToCam = projector.transform.worldToLocalMatrix;

            Vector4 localPoint = worldToCam * new Vector4(wp.x, wp.y, wp.z, 1f);
            localPoint.y = -localPoint.y;

            Vector4 temp = projIntrinsic * localPoint;

            if (temp.w == 0f)
            {
                // point is not defined
                return Vector3.zero;
            }
            else
            {
                //clip space to canvas location
                temp.x = (temp.x / temp.w + 1f) * .5f * calibData.Width - calibData.Width / 2;
                temp.y = (temp.y / temp.w + 1f) * .5f * -calibData.Height + calibData.Height/2;
                //return RemoveDistortion(temp);
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