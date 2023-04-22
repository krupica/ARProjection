using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using IO.Swagger.Model;
using Assets.Scripts.ARClasses;
//using WebSocketSharp;

namespace Base {
    public class ActionPoint : MonoBehaviour {
        [System.NonSerialized]
        public IO.Swagger.Model.ActionPoint Data = new IO.Swagger.Model.ActionPoint(id: "", robotJoints: new List<IO.Swagger.Model.ProjectRobotJoints>(), orientations: new List<IO.Swagger.Model.NamedOrientation>(), position: new IO.Swagger.Model.Position(), actions: new List<IO.Swagger.Model.Action>(), name: "");
        
        public virtual void ActionPointBaseUpdate(IO.Swagger.Model.BareActionPoint apData) {
            Data.Name = apData.Name;
            Data.Position = apData.Position;
            transform.localPosition = GetScenePosition();
        }

        public virtual void InitAP(IO.Swagger.Model.ActionPoint apData, float size) {
            Debug.Assert(apData != null);
            Data = apData;
            transform.localPosition = GetScenePosition();
        }

        public void DeleteAP(bool removeFromList = true) {
            // Remove this ActionPoint reference from parent ActionObject list
            if (removeFromList) // to allow remove all AP in foreach
                ProjectManager.Instance.ActionPoints.Remove(this.Data.Id);

            Destroy(gameObject);
        }

        public Vector3 GetScenePosition()
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
    }
}
