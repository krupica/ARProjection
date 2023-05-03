using UnityEngine;
using System.Collections.Generic;
using Base;

namespace Assets.Scripts.ARClasses
{
    public class ActionPoint : MonoBehaviour {
        [System.NonSerialized]
        public IO.Swagger.Model.ActionPoint Data = new IO.Swagger.Model.ActionPoint(id: "", robotJoints: new List<IO.Swagger.Model.ProjectRobotJoints>(), orientations: new List<IO.Swagger.Model.NamedOrientation>(), position: new IO.Swagger.Model.Position(), actions: new List<IO.Swagger.Model.Action>(), name: "");
        
        public virtual void ActionPointBaseUpdate(IO.Swagger.Model.BareActionPoint apData) {
            Data.Position = apData.Position;
            transform.localPosition = GetScenePosition();
        }

        public virtual void InitAP(IO.Swagger.Model.ActionPoint apData) {
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

        public void ResetPosition()
        {
            transform.localPosition = GetScenePosition();
        }

        public Vector3 GetScenePosition()
        {

            //Vector3 newPos = TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(Data.Position));
            //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //sphere.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            //sphere.transform.localPosition = newPos;


            Vector3 newPos = ProjectionCoordConversion.ROSToUnityY0(DataHelper.PositionToVector3(Data.Position));
            if (ProjectionManager.Instance.kinect != null)
            {
                Vector2 Point2D = ProjectionCoordConversion.ManualWorldToScreenPoint(newPos);
                return Point2D;
            }
            return newPos;
        }
    }
}
