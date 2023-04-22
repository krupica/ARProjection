using Base;
using UnityEngine;
using System.Collections.Generic;
using IO.Swagger.Model;
using TMPro;
using System;
using System.Threading.Tasks;

namespace Assets.Scripts.ARClasses
{
    public class ActionPoint2D : Base.ActionPoint
    {
        public override Vector3 GetScenePosition()
        {
            Vector3 newPos = ProjectionCoordConversion.ROSToUnityY0(DataHelper.PositionToVector3(Data.Position));
            if (ProjectionManager.Instance.kinect != null)
            {
                GameObject projector = ProjectionManager.Instance.projector;
                //Matrix4x4 kinectToWorld = kinect.transform.worldToLocalMatrix;
                //Vector3 localPoint = kinectToWorld.MultiplyPoint(newPos);

                //Vector3 Point2D = projector.GetComponent<Camera>().WorldToScreenPoint(newPos);
                Vector2 Point2D = ProjectionCoordConversion.ManualWorldToScreenPoint(newPos);
                GameObject go = Instantiate(ProjectionManager.Instance.actionPointPrefab, Point2D, Quaternion.identity, ProjectionManager.Instance.canvasScene.transform);

                return newPos;                
            }

            return newPos;
        }

        public override Quaternion GetSceneOrientation()
        {
            return Quaternion.identity;
        }

        /// <summary>
        /// Changes size of shpere representing action point
        /// </summary>
        /// <param name="size"><0; 1> - 0 means invisble, 1 means 10cm in diameter</param>
        public override void SetSize(float size)
        {
            //transform.localScale = new Vector3(size / 10, size / 10, size / 10);
        }

        public override (List<string>, Dictionary<string, string>) UpdateActionPoint(IO.Swagger.Model.ActionPoint projectActionPoint)
        {
            (List<string>, Dictionary<string, string>) result = base.UpdateActionPoint(projectActionPoint);
            //ActionPointName.text = projectActionPoint.Name;
            return result;
        }
    }
}
