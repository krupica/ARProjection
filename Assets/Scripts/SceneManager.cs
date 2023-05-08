using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.ARClasses;
using IO.Swagger.Model;
using UnityEngine;

//This class was taken from AREditor and modified for the needs of this application. (https://github.com/robofit/arcor2_areditor)
namespace Base
{
    /// <summary>
    /// Takes care of currently opened scene
    /// </summary>
    public class SceneManager : Singleton<SceneManager> {
        /// <summary>
        /// Holds all action objects in scene
        /// </summary>
        public Dictionary<string, ActionObject> ActionObjects = new Dictionary<string, ActionObject>();
        /// <summary>
        /// Spawn point for new action objects (Canvas).
        /// </summary>
        public GameObject CanvasScene;
        /// <summary>
        /// Spawn point for kinect and projector.
        /// </summary>
        public GameObject World;
        /// <summary>
        /// Prefab for Kinect
        /// </summary>        
        public GameObject KinectPrefab;
        /// <summary>
        /// Prefab for action object
        /// </summary>
        public GameObject ActionObjectPrefab;

        /// <summary>
        /// Creates scene from given json
        /// </summary>
        /// <param name="scene">Json describing scene.</param>
        /// <param name="loadResources">Indicates if resources should be loaded from server.</param>
        /// <param name="customCollisionModels">Allows to override collision models with different ones. Usable e.g. for
        /// project running screen.</param>
        /// <returns>True if scene successfully created, false otherwise</returns>
        public bool CreateScene(IO.Swagger.Model.Scene scene, CollisionModels customCollisionModels = null) {
            UpdateActionObjects(scene, customCollisionModels);
            return true;
        }

        /// <summary>
        /// Destroys scene and all objects
        /// </summary>
        /// <returns>True if scene successfully destroyed, false otherwise</returns>
        public bool DestroyScene() {
            RemoveActionObjects();
            ProjectionManager.Instance.DestroyProjection();
            //SelectorMenu.Instance.SelectorItems.Clear();
            //SceneMeta = null;
            return true;
        }

        #region ACTION_OBJECTS
        /// <summary>
        /// Spawns new action object
        /// </summary>
        /// <param name="id">UUID of action object</param>
        /// <param name="type">Action object type</param>
        /// <param name="customCollisionModels">Allows to override collision model of spawned action objects</param>
        /// <returns>Spawned action object</returns>
        public ActionObject SpawnActionObject(IO.Swagger.Model.SceneObject sceneObject, CollisionModels customCollisionModels = null) {
            if (!ActionsManager.Instance.ActionObjectsMetadata.TryGetValue(sceneObject.Type, out ActionObjectMetadata aom)) {
                return null;
            }
            GameObject obj;
            if (!aom.HasPose)
            {
                return null;
            }
            if (aom.Type == "KinectAzure")
            {
                obj = Instantiate(KinectPrefab, World.transform);
                ProjectionManager.Instance.SetupProjection(obj);
            }
            else
            {
                obj = Instantiate(ActionObjectPrefab, CanvasScene.transform);
            }

            ActionObject actionObject = obj.GetComponent<ActionObject>();
            actionObject.InitActionObject(sceneObject, obj.transform.localPosition, obj.transform.localRotation, aom, customCollisionModels);

            // Add the Action Object into scene reference
            ActionObjects.Add(sceneObject.Id, actionObject);
            actionObject.ActionObjectUpdate(sceneObject);

            return actionObject;
        }

        /// <summary>
        /// Updates action object in scene
        /// </summary>
        /// <param name="sceneObject">Description of action object</param>
        public void SceneObjectUpdated(SceneObject sceneObject) {
            ActionObject actionObject = GetActionObject(sceneObject.Id);
            if (actionObject != null) {
                actionObject.ActionObjectUpdate(sceneObject);
            } else {
                Debug.LogError("Object " + sceneObject.Name + "(" + sceneObject.Id + ") not found");
            }
        }

        /// <summary>
        /// Adds action object to scene
        /// </summary>
        /// <param name="sceneObject">Description of action object</param>
        /// <returns></returns>
        public void SceneObjectAdded(SceneObject sceneObject) {
            SpawnActionObject(sceneObject);
        }

        /// <summary>
        /// Removes action object from scene
        /// </summary>
        /// <param name="sceneObject">Description of action object</param>
        public void SceneObjectRemoved(SceneObject sceneObject) {
            ActionObject actionObject = GetActionObject(sceneObject.Id);
            if (actionObject != null) {
                ActionObjects.Remove(sceneObject.Id);
                actionObject.DeleteActionObject();
            } else {
                Debug.LogError("Object " + sceneObject.Name + "(" + sceneObject.Id + ") not found");
            }
        }

        /// <summary>
        /// Updates action GameObjects in ActionObjects dict based on the data present in IO.Swagger.Model.Scene Data.
        /// </summary>
        /// <param name="scene">Scene description</param>
        /// <param name="customCollisionModels">Allows to override action object collision model</param>
        /// <returns></returns>
        public void UpdateActionObjects(Scene scene, CollisionModels customCollisionModels = null) {
            foreach (IO.Swagger.Model.SceneObject aoSwagger in scene.Objects) {
                SpawnActionObject(aoSwagger, customCollisionModels);
                //actionObject.ActionObjectUpdate(aoSwagger);
            }
        }

        /// <summary>
        /// Destroys and removes references to all action objects in the scene.
        /// </summary>
        public void RemoveActionObjects() {
            foreach (string actionObjectId in ActionObjects.Keys.ToList<string>()) {
                RemoveActionObject(actionObjectId);
            }
            // just to make sure that none reference left
            ActionObjects.Clear();
        }

        /// <summary>
        /// Destroys and removes references to action object of given Id.
        /// </summary>
        /// <param name="Id">Action object ID</param>
        public void RemoveActionObject(string Id) {
            try {
                ActionObjects[Id].DeleteActionObject();
            } catch (NullReferenceException e) {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Finds action object by ID or throws KeyNotFoundException.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionObject GetActionObject(string id) {
            if (ActionObjects.TryGetValue(id, out ActionObject actionObject))
                return actionObject;
            throw new KeyNotFoundException("Action object not found");
        }

        public List<ActionObject> GetAllObjectsOfType(string type) {
            return ActionObjects.Values.Where(obj => obj.ActionObjectMetadata.Type == type).ToList();
        }

        #endregion
    }
}
