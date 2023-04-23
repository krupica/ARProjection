using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Swagger.Model;
using System;

namespace Base {
    public abstract class ActionObject : MonoBehaviour {

        [System.NonSerialized]
        public int CounterAP = 0;

        public IO.Swagger.Model.SceneObject Data = new IO.Swagger.Model.SceneObject(id: "", name: "", pose: DataHelper.CreatePose(new Vector3(), new Quaternion()), type: "");
        public ActionObjectMetadata ActionObjectMetadata;

        public Dictionary<string, Parameter> Overrides = new Dictionary<string, Parameter>();


        public virtual void InitActionObject(IO.Swagger.Model.SceneObject sceneObject, Vector3 position, Quaternion orientation, ActionObjectMetadata actionObjectMetadata, IO.Swagger.Model.CollisionModels customCollisionModels = null, bool loadResuources = true) {
            Data.Id = sceneObject.Id;
            Data.Type = sceneObject.Type;
            name = sceneObject.Name; // show actual object name in unity hierarchy
            ActionObjectMetadata = actionObjectMetadata;
            if (actionObjectMetadata.HasPose) {
                SetScenePosition(position);
                SetSceneOrientation(orientation);
            }
            CreateModel(customCollisionModels);
            enabled = true;           
        }
        
        public virtual void UpdateObjectName(string newUserId) {
            Data.Name = newUserId;
           // SelectorItem.SetText(newUserId);
        }

        protected virtual void Update() {
            if (ActionObjectMetadata != null && ActionObjectMetadata.HasPose && gameObject.transform.hasChanged) {
                transform.hasChanged = false;
            }
        }

        public virtual void ActionObjectUpdate(IO.Swagger.Model.SceneObject actionObjectSwagger) {
            if (Data != null & Data.Name != actionObjectSwagger.Name)
                UpdateObjectName(actionObjectSwagger.Name);
            Data = actionObjectSwagger;
            foreach (IO.Swagger.Model.Parameter p in Data.Parameters) {

                //if (!ObjectParameters.ContainsKey(p.Name)) {
                //    if (TryGetParameterMetadata(p.Name, out ParameterMeta parameterMeta)) {
                //        ObjectParameters[p.Name] = new Parameter(parameterMeta, p.Value);
                //    } else {
                //        Debug.LogError("Failed to load metadata for parameter " + p.Name);
                //        Notifications.Instance.ShowNotification("Critical error", "Failed to load parameter's metadata.");
                //        return;
                //    }

                //} else {
                //    ObjectParameters[p.Name].Value = p.Value;
                //}
            }
        }

        public void ResetPosition() {
            transform.localPosition = GetScenePosition();
            transform.localRotation = GetSceneOrientation();
        }
                
        public abstract Vector3 GetScenePosition();

        public abstract void SetScenePosition(Vector3 position);

        public abstract Quaternion GetSceneOrientation();

        public abstract void SetSceneOrientation(Quaternion orientation);

        public string GetProviderName() {
            return Data.Name;
        }

        public ActionMetadata GetActionMetadata(string action_id) {
            if (ActionObjectMetadata.ActionsLoaded) {
                if (ActionObjectMetadata.ActionsMetadata.TryGetValue(action_id, out ActionMetadata actionMetadata)) {
                    return actionMetadata;
                } else {
                    throw new ItemNotFoundException("Metadata not found");
                }
            }
            return null; //TODO: throw exception
        }


        public bool IsRobot() {
            return ActionObjectMetadata.Robot;
        }

        public bool IsCamera() {
            return ActionObjectMetadata.Camera;
        }

        public virtual void DeleteActionObject() {
            // Remove all actions of this action point
            RemoveActionPoints();
            
            // Remove this ActionObject reference from the scene ActionObject list
            SceneManager.Instance.ActionObjects.Remove(this.Data.Id);

            //DestroyObject();
            Destroy(gameObject);
        }

        public void DestroyObject() {
            DestroyObject();
        }

        public void RemoveActionPoints() {
            // Remove all action points of this action object
            List<ActionPoint> actionPoints = GetActionPoints();
            foreach (ActionPoint actionPoint in actionPoints) {
                actionPoint.DeleteAP();
            }
        }

        public string GetProviderId() {
            return Data.Id;
        }

        public abstract void UpdateModel();

        public List<ActionPoint> GetActionPoints() {
            List<ActionPoint> actionPoints = new List<ActionPoint>();
            foreach (ActionPoint actionPoint in ProjectManager.Instance.ActionPoints.Values) {
                if (actionPoint.Data.Parent == Data.Id) {
                    actionPoints.Add(actionPoint);
                }
            }
            return actionPoints;
        }

        public string GetName() {
            return Data.Name;
        }

      
        public bool IsActionObject() {
            return true;
        }

        public Base.ActionObject GetActionObject() {
            return this;
        }


        public Transform GetTransform() {
            return transform;
        }

        public string GetProviderType() {
            return Data.Type;
        }

        public GameObject GetGameObject() {
            return gameObject;
        }

        public string GetId() {
            return Data.Id;
        }

        public abstract void CreateModel(IO.Swagger.Model.CollisionModels customCollisionModels = null);

        public IO.Swagger.Model.Pose GetPose() {
            if (ActionObjectMetadata.HasPose)
                return new IO.Swagger.Model.Pose(position: DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(transform.localPosition)),
                    orientation: DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(transform.localRotation)));
            else
                return new IO.Swagger.Model.Pose(orientation: new IO.Swagger.Model.Orientation(), position: new IO.Swagger.Model.Position());
        }

        public Transform GetSpawnPoint() {
            return transform;
        }
    }
}
