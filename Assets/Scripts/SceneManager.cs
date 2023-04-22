using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.ARClasses;
using IO.Swagger.Model;
using UnityEngine;
using UnityEngine.Networking;
//using WebSocketSharp;
using static Base.GameManager;

namespace Base {
   
    /// <summary>
    /// Takes care of currently opened scene
    /// </summary>
    public class SceneManager : Singleton<SceneManager> {
        /// <summary>
        /// Holds all action objects in scene
        /// </summary>
        public Dictionary<string, ActionObject> ActionObjects = new Dictionary<string, ActionObject>();
        /// <summary>
        /// Spawn point for new action objects. Typically scene origin.
        /// </summary>
        public GameObject ActionObjectsSpawn;
        /// <summary>
        /// Prefab for Kinect
        /// </summary>        
        public GameObject KinectPrefab;
        /// <summary>
        /// Prefab for robot action object
        /// </summary>        
        public GameObject RobotPrefab;
        /// <summary>
        /// Prefab for action object
        /// </summary>
        public GameObject ActionObjectPrefab;
        /// <summary>
        /// Prefab for collision object
        /// </summary>
        public GameObject CollisionObjectPrefab;

        /// <summary>
        /// Defines if scene was started on server - e.g. if all robots and other action objects
        /// are instantioned and are ready
        /// </summary>
        private bool sceneStarted = false;

        /// <summary>
        /// Creates scene from given json
        /// </summary>
        /// <param name="scene">Json describing scene.</param>
        /// <param name="loadResources">Indicates if resources should be loaded from server.</param>
        /// <param name="customCollisionModels">Allows to override collision models with different ones. Usable e.g. for
        /// project running screen.</param>
        /// <returns>True if scene successfully created, false otherwise</returns>
        public bool CreateScene(IO.Swagger.Model.Scene scene, CollisionModels customCollisionModels = null) {
            Debug.Assert(ActionsManager.Instance.ActionsReady);
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

        /// <summary>
        /// Initialization of scene manager
        /// </summary>
        private void Start() {
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
            if (aom.Robot) {
                obj = Instantiate(RobotPrefab, ActionObjectsSpawn.transform);
            } else if (aom.CollisionObject) {
                obj = Instantiate(CollisionObjectPrefab, ActionObjectsSpawn.transform);
            } else{
                if (aom.Type == "KinectAzure")
                {
                    obj = Instantiate(KinectPrefab, ActionObjectsSpawn.transform);
                    ProjectionManager.Instance.SetupProjection(obj);
                }
                else
                {
                    obj = Instantiate(ActionObjectPrefab, ActionObjectsSpawn.transform);
                }
            } 
            ActionObject actionObject = obj.GetComponent<ActionObject>();
            actionObject.InitActionObject(sceneObject, obj.transform.localPosition, obj.transform.localRotation, aom, customCollisionModels);

            // Add the Action Object into scene reference
            ActionObjects.Add(sceneObject.Id, actionObject);
            actionObject.ActionObjectUpdate(sceneObject);

            return actionObject;
        }

        /// <summary>
        /// Transform string to underscore case (e.g. CamelCase to camel_case)
        /// </summary>
        /// <param name="str">String to be transformed</param>
        /// <returns>Underscored string</returns>
        public static string ToUnderscoreCase(string str) {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
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
        /// Gets next action object in dictionary. Allows to iterate through all action objects
        /// </summary>
        /// <param name="aoId">Current action object UUID</param>
        /// <returns></returns>
        public ActionObject GetNextActionObject(string aoId) {
            List<string> keys = ActionObjects.Keys.ToList();
            Debug.Assert(keys.Count > 0);
            int index = keys.IndexOf(aoId);
            string next;
            if (index + 1 < ActionObjects.Keys.Count)
                next = keys[index + 1];
            else
                next = keys[0];
            if (!ActionObjects.TryGetValue(next, out ActionObject actionObject)) {
                throw new ItemNotFoundException("This should never happen");
            }
            return actionObject;
        }

        /// <summary>
        /// Gets previous action object in dictionary. Allows to iterate through all action objects
        /// </summary>
        /// <param name="aoId">Current action object UUID</param>
        /// <returns></returns>
        public ActionObject GetPreviousActionObject(string aoId) {
            List<string> keys = ActionObjects.Keys.ToList();
            Debug.Assert(keys.Count > 0);
            int index = keys.IndexOf(aoId);
            string previous;
            if (index - 1 > -1)
                previous = keys[index - 1];
            else
                previous = keys[keys.Count - 1];
            if (!ActionObjects.TryGetValue(previous, out ActionObject actionObject)) {
                throw new ItemNotFoundException("This should never happen");
            }
            return actionObject;
        }

        /// <summary>
        /// Gets first action object in dictionary all null if empty
        /// </summary>
        /// <returns></returns>
        public ActionObject GetFirstActionObject() {
            if (ActionObjects.Count == 0) {
                return null;
            }
            return ActionObjects.First().Value;
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
            if (ActionObjects.TryGetValue(id, out Base.ActionObject actionObject))
                return actionObject;
            throw new KeyNotFoundException("Action object not found");
        }

        /// <summary>
        /// Tries to get action object based on its human readable name
        /// </summary>
        /// <param name="name">Human readable name</param>
        /// <param name="actionObjectOut">Found action object</param>
        /// <returns>True if object was found, false otherwise</returns>
        public bool TryGetActionObjectByName(string name, out ActionObject actionObjectOut) {
            foreach (ActionObject actionObject in ActionObjects.Values) {
                if (actionObject.GetName() == name) {
                    actionObjectOut = actionObject;
                    return true;
                }   
            }
            actionObjectOut = null;
            return false;
        }

        /// <summary>
        /// Checks if there is action object of given name
        /// </summary>
        /// <param name="name">Human readable name of actio point</param>
        /// <returns>True if action object with given name exists, false otherwise</returns>
        public bool ActionObjectsContainName(string name) {
            foreach (ActionObject actionObject in ActionObjects.Values) {
                if (actionObject.Data.Name == name) {
                    return true;
                }
            }
            return false;
        }

        public List<ActionObject> GetAllObjectsOfType(string type) {
            return ActionObjects.Values.Where(obj => obj.ActionObjectMetadata.Type == type).ToList();
        }

        #endregion
    }
}
