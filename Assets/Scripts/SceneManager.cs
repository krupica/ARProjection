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
        /// Contains metainfo about scene (id, name, modified etc) without info about objects and services
        /// </summary>
        public Scene SceneMeta = null;
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
        /// Indicates if resources (e.g. end effectors for robot) should be loaded when scene created.
        /// </summary>
        private bool loadResources = false;

        /// <summary>
        /// Defines if scene was started on server - e.g. if all robots and other action objects
        /// are instantioned and are ready
        /// </summary>
        private bool sceneStarted = false;

        /// <summary>
        /// Flag which indicates whether scene update event should be trigered during update
        /// </summary>
        private bool updateScene = false;

        public event AREditorEventArgs.SceneStateHandler OnSceneStateEvent;

        public string SelectedArmId;

        //private RobotEE selectedEndEffector;

        public bool Valid = false;
        /// <summary>
        /// Public setter for sceneChanged property. Invokes OnSceneChanged event with each change and
        /// OnSceneSavedStatusChanged when sceneChanged value differs from original value (i.e. when scene
        /// was not changed and now it is and vice versa)
        /// </summary>
        public bool SceneStarted {
            get => sceneStarted;
            private set => sceneStarted = value;
        }
        //public RobotEE SelectedEndEffector {
        //    get => selectedEndEffector;
        //    set => selectedEndEffector = value;
        //}

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
            if (SceneMeta != null)
                return false;
            SetSceneMeta(DataHelper.SceneToBareScene(scene));            
            LoadSettings();

            UpdateActionObjects(scene, customCollisionModels);

            Valid = true;
            return true;
        }

        /// <summary>
        /// Destroys scene and all objects
        /// </summary>
        /// <returns>True if scene successfully destroyed, false otherwise</returns>
        public bool DestroyScene() {
            SceneStarted = false;
            Valid = false;
            RemoveActionObjects();
            ProjectionManager.Instance.DestroyProjection();
            //SelectorMenu.Instance.SelectorItems.Clear();
            SceneMeta = null;
            return true;
        }

        /// <summary>
        /// Sets scene metadata
        /// </summary>
        /// <param name="scene">Scene metadata</param>
        public void SetSceneMeta(BareScene scene) {
            if (SceneMeta == null) {
                SceneMeta = new Scene(id: "", name: "");
            }
            SceneMeta.Id = scene.Id;
            SceneMeta.Description = scene.Description;
            SceneMeta.IntModified = scene.IntModified;
            SceneMeta.Modified = scene.Modified;
            SceneMeta.Name = scene.Name;
        }

        /// <summary>
        /// Gets scene metadata.
        /// </summary>
        /// <returns></returns>
        public IO.Swagger.Model.Scene GetScene() {
            if (SceneMeta == null)
                return null;
            Scene scene = SceneMeta;
            scene.Objects = new List<SceneObject>();
            foreach (ActionObject o in ActionObjects.Values) {
                scene.Objects.Add(o.Data);
            }
            return scene;
        }
        
        /// <summary>
        /// Initialization of scene manager
        /// </summary>
        private void Start() {
            WebsocketManager.Instance.OnSceneBaseUpdated += OnSceneBaseUpdated;
            WebsocketManager.Instance.OnSceneStateEvent += OnSceneState;
            WebsocketManager.Instance.OnOverrideAdded += OnOverrideAddedOrUpdated;
            WebsocketManager.Instance.OnOverrideUpdated += OnOverrideAddedOrUpdated;
            WebsocketManager.Instance.OnOverrideBaseUpdated += OnOverrideAddedOrUpdated;
            WebsocketManager.Instance.OnOverrideRemoved += OnOverrideRemoved;
        }

        private void OnOverrideRemoved(object sender, ParameterEventArgs args) {
            try {
                ActionObject actionObject = GetActionObject(args.ObjectId);
                actionObject.Overrides.Remove(args.Parameter.Name);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);

            }
        }

        private void OnOverrideAddedOrUpdated(object sender, ParameterEventArgs args) {

            try {
                ActionObject actionObject = GetActionObject(args.ObjectId);
                if (actionObject.TryGetParameterMetadata(args.Parameter.Name, out ParameterMeta parameterMeta)) {
                    //Parameter p = new Parameter(parameterMeta, args.Parameter.Value);
                    //actionObject.Overrides[args.Parameter.Name] = p;
                }
                
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                
            }
        }

        private async void OnSceneState(object sender, SceneStateEventArgs args) {
            switch (args.Event.State) {
                case SceneStateData.StateEnum.Starting:
                    OnSceneStateEvent?.Invoke(this, args); // needs to be rethrown to ensure all subscribers has updated data
                    break;
                case SceneStateData.StateEnum.Stopping:
                    SceneStarted = false;
                    if (!string.IsNullOrEmpty(args.Event.Message)) {
                        Notifications.Instance.ShowNotification("Scene service failed", args.Event.Message);
                    }
                    OnSceneStateEvent?.Invoke(this, args); // needs to be rethrown to ensure all subscribers has updated data
                    break;
                case SceneStateData.StateEnum.Started:
                    StartCoroutine(WaitUntillSceneValid(() => OnSceneStarted(args)));
                    break;
                case SceneStateData.StateEnum.Stopped:
                    SceneStarted = false;
                    SelectedArmId = null;
                    //SelectedEndEffector = null;
                    OnSceneStateEvent?.Invoke(this, args); // needs to be rethrown to ensure all subscribers has updated data
                    break;
            }
        }

        private IEnumerator WaitUntillSceneValid(UnityEngine.Events.UnityAction callback) {
            yield return new WaitUntil(() => Valid);
            callback();
        }

        private async void OnSceneStarted(SceneStateEventArgs args) {
            SceneStarted = true;
            //RegisterRobotsForEvent(true, RegisterForRobotEventRequestArgs.WhatEnum.Joints);
            string selectedRobotID = PlayerPrefsHelper.LoadString(SceneMeta.Id + "/selectedRobotId", null);
            SelectedArmId = PlayerPrefsHelper.LoadString(SceneMeta.Id + "/selectedRobotArmId", null);
            string selectedEndEffectorId = PlayerPrefsHelper.LoadString(SceneMeta.Id + "/selectedEndEffectorId", null);
            //await SelectRobotAndEE(selectedRobotID, SelectedArmId, selectedEndEffectorId);
            //GameManager.Instance.HideLoadingScreen();
            OnSceneStateEvent?.Invoke(this, args); // needs to be rethrown to ensure all subscribers has updated data
        }

        private void OnSceneBaseUpdated(object sender, BareSceneEventArgs args) {
            if (GameManager.Instance.GetGameState() == GameManager.GameStateEnum.SceneEditor) {
                SetSceneMeta(args.Scene);
                updateScene = true;
            }
        }

        /// <summary>
        /// Loads selected setings from player prefs
        /// </summary>
        internal void LoadSettings() {
            //ActionObjectsVisibility = PlayerPrefsHelper.LoadFloat("AOVisibility" + (VRModeManager.Instance.VRModeON ? "VR" : "AR"), (VRModeManager.Instance.VRModeON ? 1f : 0f));
            //ActionObjectsInteractive = PlayerPrefsHelper.LoadBool("scene/" + SceneMeta.Id + "/AOInteractivity", true);
            //RobotsEEVisible = PlayerPrefsHelper.LoadBool("scene/" + SceneMeta.Id + "/RobotsEEVisibility", true);
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
        /// Finds free action object name, based on action object type (e.g. Box, Box_1, Box_2 etc.)
        /// </summary>
        /// <param name="aoType">Type of action object</param>
        /// <returns></returns>
        public string GetFreeAOName(string aoType) {
            int i = 1;
            bool hasFreeName;
            string freeName = ToUnderscoreCase(aoType);
            do {
                hasFreeName = true;
                if (ActionObjectsContainName(freeName)) {
                    hasFreeName = false;
                }
                if (!hasFreeName)
                    freeName = ToUnderscoreCase(aoType) + "_" + i++.ToString();
            } while (!hasFreeName);

            return freeName;
        }

        public string GetFreeObjectTypeName(string objectTypeName) {
            int i = 1;
            bool hasFreeName;
            string freeName = objectTypeName;
            do {
                hasFreeName = true;
                if (ActionsManager.Instance.ActionObjectsMetadata.ContainsKey(freeName)) {
                    hasFreeName = false;
                }
                if (!hasFreeName)
                    freeName = ToUnderscoreCase(objectTypeName) + "_" + i++.ToString();
            } while (!hasFreeName);

            return freeName;
        }

        public string GetFreeSceneName(string sceneName) {
            int i = 1;
            bool hasFreeName;
            string freeName = sceneName;
            do {
                hasFreeName = true;
                try {
                    GameManager.Instance.GetSceneId(freeName);
                    hasFreeName = false;
                    freeName = sceneName + "_" + i++.ToString();
                } catch (RequestFailedException) {
                    
                }
                    
            } while (!hasFreeName);

            return freeName;
        }

        /// <summary>
        /// Returns all robots in scene
        /// </summary>
        /// <returns></returns>
        //public List<IRobot> GetRobots() {
        //    List<IRobot> robots = new List<IRobot>();
        //    foreach (ActionObject actionObject in ActionObjects.Values) {
        //        if (actionObject.IsRobot()) {
        //            //robots.Add((RobotActionObject) actionObject);
        //        }                    
        //    }
        //    return robots;
        //}

        public List<ActionObject> GetCameras() {
            List<ActionObject> cameras = new List<ActionObject>();
            foreach (ActionObject actionObject in ActionObjects.Values) {
                if (actionObject.IsCamera()) {
                    cameras.Add(actionObject);
                }
            }
            return cameras;
        }

        public List<string> GetCamerasIds() {
            List<string> cameraIds = new List<string>();
            foreach (ActionObject actionObject in ActionObjects.Values) {
                if (actionObject.IsCamera()) {
                    cameraIds.Add(actionObject.Data.Id);
                }
            }
            return cameraIds;
        }

        public List<string> GetCamerasNames() {
            List<string> camerasNames = new List<string>();
            foreach (ActionObject actionObject in ActionObjects.Values) {
                if (actionObject.IsCamera()) {
                    camerasNames.Add(actionObject.Data.Name);
                }
            }
            return camerasNames;
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
        /// Updates metadata of action object in scene
        /// </summary>
        /// <param name="sceneObject">Description of action object</param>
        public void SceneObjectBaseUpdated(SceneObject sceneObject) {
            ActionObject actionObject = GetActionObject(sceneObject.Id);
            if (actionObject != null) {

            } else {
                Debug.LogError("Object " + sceneObject.Name + "(" + sceneObject.Id + ") not found");
            }
            updateScene = true;
        }

        /// <summary>
        /// Adds action object to scene
        /// </summary>
        /// <param name="sceneObject">Description of action object</param>
        /// <returns></returns>
        public void SceneObjectAdded(SceneObject sceneObject) {
            ActionObject actionObject = SpawnActionObject(sceneObject);
            updateScene = true;
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
            updateScene = true;
        }

        /// <summary>
        /// Updates action GameObjects in ActionObjects dict based on the data present in IO.Swagger.Model.Scene Data.
        /// </summary>
        /// <param name="scene">Scene description</param>
        /// <param name="customCollisionModels">Allows to override action object collision model</param>
        /// <returns></returns>
        public void UpdateActionObjects(Scene scene, CollisionModels customCollisionModels = null) {
            List<string> currentAO = new List<string>();
            foreach (IO.Swagger.Model.SceneObject aoSwagger in scene.Objects) {
                ActionObject actionObject = SpawnActionObject(aoSwagger, customCollisionModels);
                //actionObject.ActionObjectUpdate(aoSwagger);
                currentAO.Add(aoSwagger.Id);
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

        public List<ActionObject> GetAllActionObjectsWithoutPose() {
            List<ActionObject> objects = new List<ActionObject>();
            foreach (ActionObject actionObject in ActionObjects.Values) {
                if (!actionObject.ActionObjectMetadata.HasPose && actionObject.gameObject.activeSelf) {
                    objects.Add(actionObject);
                }
            }
            return objects;
        }

        //public async Task<List<RobotEE>> GetAllRobotsEEs() {
        //    List<RobotEE> eeList = new List<RobotEE>();
        //    foreach (ActionObject ao in ActionObjects.Values) {
        //        if (ao.IsRobot())
        //            eeList.AddRange(await ((IRobot) ao).GetAllEE());
        //    }
        //    return eeList;
        //}

        public List<ActionObject> GetAllObjectsOfType(string type) {
            return ActionObjects.Values.Where(obj => obj.ActionObjectMetadata.Type == type).ToList();
        }

        #endregion
    }
}
