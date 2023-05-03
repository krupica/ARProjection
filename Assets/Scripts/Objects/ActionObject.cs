using UnityEngine;
using System.Collections.Generic;
using IO.Swagger.Model;
using Base;
using UnityEngine.UI;
using System;

namespace Assets.Scripts.ARClasses
{
    public class ActionObject : MonoBehaviour {

        public IO.Swagger.Model.SceneObject Data = new IO.Swagger.Model.SceneObject(id: "", name: "", pose: DataHelper.CreatePose(new Vector3(), new Quaternion()), type: "");
        public ActionObjectMetadata ActionObjectMetadata;

        public Color ActionObjectColor;
        public Color CollisionObjectColor;
        public Color RobotColor;

        [SerializeField]
        public GameObject Square, Circle;
        public GameObject Model;

        public virtual void InitActionObject(IO.Swagger.Model.SceneObject sceneObject, Vector3 position, Quaternion orientation, ActionObjectMetadata actionObjectMetadata, IO.Swagger.Model.CollisionModels customCollisionModels = null, bool loadResuources = true) {
            Data.Id = sceneObject.Id;
            Data.Type = sceneObject.Type;
            name = sceneObject.Name; // show actual object name in unity hierarchy
            ActionObjectMetadata = actionObjectMetadata;
            if (actionObjectMetadata.HasPose) {
                SetScenePosition(position);
                SetSceneOrientation(orientation);
            }
            CreateModel();            
            enabled = true;
        }

        public void UpdateModel()
        {
            if (ActionObjectMetadata.ObjectModel == null)
                return;
            Vector3? dimensions = null;
            switch (ActionObjectMetadata.ObjectModel.Type)
            {
                case ObjectModel.TypeEnum.Box:
                    dimensions = ProjectionCoordConversion.ROSToCanvasScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Box.SizeX, (float)ActionObjectMetadata.ObjectModel.Box.SizeY, (float)ActionObjectMetadata.ObjectModel.Box.SizeZ));
                    break;
                case ObjectModel.TypeEnum.Sphere:
                    dimensions = ProjectionCoordConversion.ROSToCanvasScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius));
                    break;
                case ObjectModel.TypeEnum.Cylinder:
                    dimensions = ProjectionCoordConversion.ROSToCanvasScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float)ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float)ActionObjectMetadata.ObjectModel.Cylinder.Height));
                    break;

            }
            if (dimensions != null)
                Model.transform.localScale = new Vector3(dimensions.Value.x, dimensions.Value.y, dimensions.Value.z);
        }

        public virtual void CreateModel()
        {
            if (ActionObjectMetadata.ObjectModel==null)
            {
                Model = Instantiate(Square, transform);
                Model.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                UpdateColor();
                return;
            }
            switch (ActionObjectMetadata.ObjectModel.Type)
            {
                case IO.Swagger.Model.ObjectModel.TypeEnum.Box:
                    Model = Instantiate(Square, transform);
                    Model.transform.localScale = ProjectionCoordConversion.ROSToCanvasScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Box.SizeX, (float)ActionObjectMetadata.ObjectModel.Box.SizeY, (float)ActionObjectMetadata.ObjectModel.Box.SizeZ));
                    break;

                case IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder:
                    Model = Instantiate(Circle, transform);
                    Model.transform.localScale = new Vector3((float)ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float)ActionObjectMetadata.ObjectModel.Cylinder.Height / 2, (float)ActionObjectMetadata.ObjectModel.Cylinder.Radius);
                    break;

                case IO.Swagger.Model.ObjectModel.TypeEnum.Sphere:
                    Model = Instantiate(Circle, transform);
                    Model.transform.localScale = new Vector3((float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius);
                    break;
                default:
                    Model = Instantiate(Square, transform);
                    Model.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                    break;
            }
            UpdateColor();
        }

        public virtual void UpdateColor()
        {
            var image = Model.GetComponent<Image>();
            if (ActionObjectMetadata.CollisionObject)
            {
                image.color = CollisionObjectColor;
            }
            else
            {
                if (ActionObjectMetadata.Robot)
                {
                    image.color = RobotColor;
                }
                else
                {
                    image.color = ActionObjectColor;
                }
            }
        }

        public virtual void ActionObjectUpdate(IO.Swagger.Model.SceneObject actionObjectSwagger) {
            Data = actionObjectSwagger;
            ResetPosition();
        }

        public void ResetPosition() {
            if(ActionObjectMetadata.HasPose)
            {
                transform.localPosition = GetScenePosition();
                transform.localRotation = GetSceneOrientation();
            }
        }

        public virtual Vector3 GetScenePosition()
        {
            if (!ActionObjectMetadata.HasPose)
            {
                return Vector3.zero;
            }

            Vector3 newPos = ProjectionCoordConversion.ROSToUnityY0(DataHelper.PositionToVector3(Data.Pose.Position));
            if (ProjectionManager.Instance.kinect != null)
            {
                Vector2 Point2D = ProjectionCoordConversion.ManualWorldToScreenPoint(newPos);
                return Point2D;
            }
            return newPos;
        }

        public virtual void SetScenePosition(Vector3 position)
        {
            Data.Pose.Position = DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(position));
        }

        public virtual Quaternion GetSceneOrientation()
        {
            return ProjectionCoordConversion.ROSToCanvas(DataHelper.OrientationToQuaternion(Data.Pose.Orientation));
        }

        public virtual void SetSceneOrientation(Quaternion orientation)
        {
            Data.Pose.Orientation = DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(orientation));
        }

        public virtual void DeleteActionObject() {
            RemoveActionPoints();
            // Remove this ActionObject reference from the scene ActionObject list
            SceneManager.Instance.ActionObjects.Remove(Data.Id);
            Destroy(gameObject);
        }

        public void RemoveActionPoints()
        {
            // Remove all action points of this action object
            List<ActionPoint> actionPoints = GetActionPoints();
            foreach (ActionPoint actionPoint in actionPoints)
            {
                actionPoint.DeleteAP();
            }
        }

        public List<ActionPoint> GetActionPoints() {
            List<ActionPoint> actionPoints = new List<ActionPoint>();
            foreach (ActionPoint actionPoint in ProjectManager.Instance.ActionPoints.Values) {
                if (actionPoint.Data.Parent == Data.Id) {
                    actionPoints.Add(actionPoint);
                }
            }
            return actionPoints;
        }
    }
}
